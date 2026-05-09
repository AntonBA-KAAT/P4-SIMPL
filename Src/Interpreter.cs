using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;

public sealed class Interpreter
{
    private readonly ProgramNode _program;
    private readonly Dictionary<string, FunctionNode> _functions;
    private readonly ConcurrentDictionary<int, BlockingCollection<Message>> _mailboxes = new();
    private int _nextPid = 0;

    private sealed class ReturnSignal : Exception
    {
        public RuntimeValues Value {get;}
        public ReturnSignal(RuntimeValues value)
        {
            Value = value;
        }
    }

    public Interpreter(ProgramNode program)
    {
        _program = program;
        _functions = program.Functions.ToDictionary(f => f.Name, f => f);
    }
    public RuntimeValues Run()
    {
        if(!_functions.TryGetValue("main", out var main))
        {
            throw  new RuntimeException("No main function found.");
        }
        var mainPid = AllocatePid();
        _mailboxes[mainPid] = new BlockingCollection<Message>();

        var mainProcess = new RuntimeProcess(mainPid);

        return ExecuteFunction(main, new List<RuntimeValues>(), mainProcess);
    }

    private int AllocatePid()
    {
        return Interlocked.Increment(ref _nextPid);
    }

    private RuntimeValues EvalExpr(ExprNode expr, RuntimeProcess process)
    {
        return expr switch
        {
            NumberNode n => new IntValue(n.Value),
            BoolNode b => new BoolValue(b.Value),
            SelfNode => new PidValue(process.Pid),
            VarNode v => process.Store.TryGetValue(v.Name, out var value)
                        ? value
                        : throw new RuntimeException($"Undefined variable '{v.Name}'"),
            UnaryExprNode u => EvalUnary(u, process),
            BinaryExprNode b => EvalBinary(b, process),
            CallExprNode c => throw new RuntimeException($"Function calls are not allowed as pure expressions: '{c.Name}'"),
            _ => throw new RuntimeException($"Unsupported expression node: '{expr.GetType().Name}'")
        };
    }
    
    private RuntimeValues EvalUnary(UnaryExprNode expr, RuntimeProcess process)
    {
        var value = EvalExpr(expr.Operand, process);

        return expr.Operator switch
        {
            "!" when value is BoolValue b => new BoolValue(!b.Value),
            "-" when value is IntValue i => new IntValue(-i.Value),
            _ => throw new RuntimeException($"Invalid unary operator '{expr.Operator}'")
        };
    }
    private RuntimeValues EvalBinary(BinaryExprNode expr, RuntimeProcess process)
    {
        var left = EvalExpr(expr.Left, process);
        var right = EvalExpr(expr.Right, process);

        return expr.Operator switch
        {
            "+" when left is IntValue l && right is IntValue r => new IntValue(l.Value + r.Value),
            "-" when left is IntValue l && right is IntValue r => new IntValue(l.Value - r.Value),
            "*" when left is IntValue l && right is IntValue r => new IntValue(l.Value * r.Value),
            "/" when left is IntValue l && right is IntValue r => new IntValue(l.Value / r.Value),

            "<" when left is IntValue l && right is IntValue r => new BoolValue(l.Value < r.Value),
            ">" when left is IntValue l && right is IntValue r => new BoolValue(l.Value > r.Value),
            "<=" when left is IntValue l && right is IntValue r => new BoolValue(l.Value <= r.Value),
            ">=" when left is IntValue l && right is IntValue r => new BoolValue(l.Value >= r.Value),

            "==" => new BoolValue(ValuesEqual(left, right)),
            "!=" => new BoolValue(!ValuesEqual(left, right)),

            "&&" when left is BoolValue l && right is BoolValue r => new BoolValue(l.Value && r.Value),
            "||" when left is BoolValue l && right is BoolValue r => new BoolValue(l.Value || r.Value),

            _ => throw new RuntimeException($"Invalid Binary operaotor '{expr.Operator}'")
        };
    }
    private static bool ValuesEqual(RuntimeValues left, RuntimeValues right)
    {
        return (left, right) switch
        {
            (IntValue l, IntValue r) => l.Value == r.Value,
            (BoolValue l, BoolValue r) => l.Value == r.Value,
            (PidValue l, PidValue r) => l.Value == r.Value,
            _ => false
        };
    }

    private RuntimeValues ExecuteFunction(FunctionNode function, IReadOnlyList<RuntimeValues> args, RuntimeProcess process)
    {
        if(args.Count != function.Parameters.Count)
        {
            throw new RuntimeException($"Function '{function.Name}' expected {function.Parameters.Count} arguments but got {args.Count}");
        }
        var oldStore = new Dictionary<string, RuntimeValues>(process.Store);
        process.Store.Clear();
        for(int i = 0; i < args.Count; i++)
        {
            process.Store[function.Parameters[i].Name] = args[i];
        }
        try
        {
            ExecuteStatementSequence(function.Statements, process);
            throw new RuntimeException($"Function '{function.Name}' ended without returning");
        }
        catch(ReturnSignal ret)
        {
            return ret.Value;
        }
        finally
        {
            process.Store.Clear();
            foreach(var pair in oldStore)
            {
                process.Store[pair.Key] = pair.Value;
            }
        }
    }
    private void ExecuteStatementSequence(IReadOnlyCollection<StatementNode> statements, RuntimeProcess process)
    {
        foreach(var statement in statements)
        {
            ExecuteStatement(statement, process);
        }
    }
    private void ExecuteStatement(StatementNode statement, RuntimeProcess process)
    {
        switch (statement)
        {
            case AssignNode a:
                if (!process.Store.ContainsKey(a.Name.Name))
                {
                    throw new RuntimeException($"Undefined variable '{a.Name.Name}'");
                }
                process.Store[a.Name.Name] = EvalRhs(a.Value, process);
                break;
            
            case DeclNode d:
                if (process.Store.ContainsKey(d.Name))
                {
                    throw new RuntimeException($"Variable '{d.Name}' already declared in this scope");
                }
                process.Store[d.Name] = EvalRhs(d.Value, process);
                break;
            
            case PrintNode p:
                Console.WriteLine(ValueToString(EvalExpr(p.Value, process)));
                break;
            
            case ReturnNode r:
                throw new ReturnSignal(EvalExpr(r.Value, process));

            case SendNode s:
                var message = EvalExpr(s.Message, process);
                var target = EvalExpr(s.Target, process);
                if (message is not IntValue msg)
                    throw new RuntimeException("send message must be Int");
                if (target is not PidValue pid)
                    throw new RuntimeException("send target must be Pid");
                Send(process.Pid, pid.Value, msg.Value);
                break;
            
            case SkipNode:
                break;
            
            case WhileNode w:
                while (AsBool(EvalExpr(w.Condition, process)))
                {
                    ExecuteStatementSequence(w.Body, process);
                }
                break;
            case IfNode i:
                if(AsBool(EvalExpr(i.Condition, process)))
                {
                    ExecuteStatementSequence(i.ThenBranch, process);
                }
                else
                {
                    ExecuteStatementSequence(i.ElseBranch, process);
                }
                break;
            
            default:
                throw new RuntimeException($"Unsupported statement node: {statement.GetType().Name}");
        }
    }
    private static bool AsBool(RuntimeValues value)
    {
        return value is BoolValue b
            ? b.Value
            : throw new RuntimeException("Expected bool value");
    }
    private static string ValueToString(RuntimeValues value)
    {
        return value switch
        {
            IntValue i => i.Value.ToString(),
            BoolValue b => b.Value.ToString(),
            PidValue p => $"pid({p.Value})",
            _ => value.ToString() ?? ""
        };
    }
    private RuntimeValues EvalRhs(RhsNode rhs, RuntimeProcess process)
    {
        return rhs switch
        {
            ExprRhsNode e => EvalExpr(e.Value, process),
            CallRhsNode c => EvalCall(c, process),
            SpawnRhsNode s => EvalSpawn(s, process),
            ReceiveRhsNode r => EvalReceive(r, process),
            _ => throw new RuntimeException($"Unsupported RHS node: {rhs.GetType().Name}")
        };
    }
    private RuntimeValues EvalCall(CallRhsNode rhs, RuntimeProcess process)
    {
        if(!_functions.TryGetValue(rhs.Callee, out var function))
        {
            throw new RuntimeException($"Undefined function '{rhs.Callee}'");
        }
        var args = rhs.Arguments.Select(arg => EvalExpr(arg, process)).ToList();

        return ExecuteFunction(function, args, process);
    }

    //Concurrent things
    private void Send(int SenderPid, int targetPid, int value)
    {
        if(!_mailboxes.TryGetValue(targetPid, out var mailbox))
        {
            throw new RuntimeException($"No process with pid  {targetPid}");
        }
        mailbox.Add(new Message(SenderPid, value));
    }

    private RuntimeValues EvalReceive(ReceiveRhsNode rhs, RuntimeProcess process)
    {
        var source = EvalExpr(rhs.Source, process);
        if(source is not PidValue sourcePid)
        {
            throw new RuntimeException("receive expects a Pid");
        }
        if(!_mailboxes.TryGetValue(process.Pid, out var mailbox))
        {
            throw new RuntimeException($"No mailbox for pid {process.Pid}");
        }
        var skipped = new List<Message>();

        while (true)
        {
            var msg = mailbox.Take();//blocks if no message in mailbox
            if(msg.SenderPid == sourcePid.Value)
            {
                foreach(var skippedMsg in skipped)
                {
                    mailbox.Add(skippedMsg);
                }
                return new IntValue(msg.Value);
            }
            skipped.Add(msg);
        }
    }
    private RuntimeValues EvalSpawn(SpawnRhsNode rhs, RuntimeProcess parentProcess)
    {
        if(!_functions.TryGetValue(rhs.Callee, out var function))
        {
            throw new RuntimeException($"Undefined function '{rhs.Callee}'");
        }
        var args =rhs.Arguments
            .Select(arg => EvalExpr(arg, parentProcess))
            .ToList();
        
        var childPid = AllocatePid();
        _mailboxes[childPid] = new BlockingCollection<Message>();

        var childProcess = new RuntimeProcess(childPid);
        _ = Task.Run(() =>
        {
            try
            {
                ExecuteFunction(function,args,childProcess);
            }catch(Exception ex)
            {
                Console.Error.WriteLine($"Runtime error in process {childPid}: {ex.Message}");
            }
        });
        return new PidValue(childPid);
    }
}