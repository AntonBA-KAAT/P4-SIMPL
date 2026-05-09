using System;
using System.Collections.Generic;
using System.Linq;

public abstract record BindingType;

public sealed record ValueBinding(TypeNode Type): BindingType;

public sealed record FunctionBinding(IReadOnlyList<TypeNode> ParameterTypes, TypeNode ReturnType) : BindingType;

public sealed record StatementCheckResult(
    Dictionary<string, BindingType> Env,
    bool DefinitelyReturns
);

public sealed class TypeCheckException : Exception
{
    public TypeCheckException(string message) : base(message) { }
}

public sealed class TypeChecker
{
    public void CheckProgram(ProgramNode program)
    {
        var globalEnv = BuildFunctionEnvironment(program);

        foreach (var function in program.Functions)
        {
            CheckFunction(function, globalEnv);
        }
    }
    private Dictionary<string, BindingType> BuildFunctionEnvironment(ProgramNode program)
    {
        var env = new Dictionary<string, BindingType>();

        foreach (var function in program.Functions)
        {
            if(env.ContainsKey(function.Name))
            {
                throw new TypeCheckException($"Duplicate function name: {function.Name}");
            }
            var paramTypes = function.Parameters
                .Select(p => p.ParameterType)
                .ToList();
            env[function.Name] = new FunctionBinding(paramTypes, function.ReturnType);
        }
        return env;
    }
    private void CheckFunction(FunctionNode function, Dictionary<string, BindingType> globalEnv)
    {
        var env = new Dictionary<string, BindingType>(globalEnv);
        foreach (var param in function.Parameters)
        {
            if(env.ContainsKey(param.Name))
            {
                throw new TypeCheckException($"Parameter '{param.Name}' in function '{function.Name}' conflicts with an existing identifier.");
            }
            env[param.Name] = new ValueBinding(param.ParameterType);
        }
        var result = CheckStatementSequence(function.Statements, env, function.ReturnType);

        if(!result.DefinitelyReturns)
        {
            throw new TypeCheckException($"Function '{function.Name}' may not return a value on all paths.");
        }
    }
    private TypeNode CheckExpr(ExprNode expr, Dictionary<string, BindingType> env)
    {
        return expr switch
        {
            NumberNode => TypeNode.Int,

            BoolNode => TypeNode.Bool,

            SelfNode => TypeNode.Pid,

            VarNode v => LookupVariable(v.Name, env),

            UnaryExprNode u => CheckUnaryExpr(u, env),

            BinaryExprNode b => CheckBinaryExpr(b, env),

            CallExprNode c => throw new TypeCheckException($"Call expression '{c.Name}(...)' is not allowed as a pure expression in SIMTL."),

            _ => throw new TypeCheckException($"Unsupported expression node: {expr.GetType().Name}") 
        };
    }
    private TypeNode LookupVariable(string name, Dictionary<string, BindingType> env)
    {
        if(!env.TryGetValue(name, out var binding))
        {
            throw new TypeCheckException($"Undefined variable: {name}");
        }
        return binding switch
        {
            ValueBinding vb => vb.Type,
            FunctionBinding => throw new TypeCheckException($"Identifier '{name}' is a function, not a variable."),
            _ => throw new TypeCheckException($"Invalid binding type for identifier '{name}'.")
        };
    }
    private TypeNode CheckUnaryExpr(UnaryExprNode expr, Dictionary<string, BindingType> env)
    {
        var operandType = CheckExpr(expr.Operand, env);
        return expr.Operator switch
        {
            "!" when operandType == TypeNode.Bool => TypeNode.Bool,
            "-" when operandType == TypeNode.Int => TypeNode.Int,
            _ => throw new TypeCheckException($"Invalid unary operator '{expr.Operator}' for operand type '{operandType}'.")
        };
    }
    private TypeNode CheckBinaryExpr(BinaryExprNode expr, Dictionary<string, BindingType> env)
    {
        var leftType = CheckExpr(expr.Left, env);
        var rightType = CheckExpr(expr.Right, env);

        return expr.Operator switch
        {
            "+" or "-" or "*" or "/"
                when leftType == TypeNode.Int && rightType == TypeNode.Int => TypeNode.Int,
            "<" or ">" or ">=" or "<="
                when leftType == TypeNode.Int && rightType == TypeNode.Int => TypeNode.Bool,
            "==" or "!="
                when leftType == rightType => TypeNode.Bool,
            "&&" or "||"
                when leftType == TypeNode.Bool && rightType == TypeNode.Bool => TypeNode.Bool,
            "+" or "-" or "*" or "/" =>
                throw new TypeCheckException($"Operator '{expr.Operator}' requires integer operands."),
            "<" or ">" or ">=" or "<=" =>
                throw new TypeCheckException($"Operator '{expr.Operator}' requires integer operands."),
            "==" or "!=" =>
                throw new TypeCheckException($"Operator '{expr.Operator}' requires operands of the same type."),
            
            "&&" or "||" =>
                throw new TypeCheckException($"Operator '{expr.Operator}' requires boolean operands."),
            _ => throw new TypeCheckException($"Unsupported binary operator: {expr.Operator}")
        };
    }
    private TypeNode CheckRhs(RhsNode rhs, Dictionary<string, BindingType> env)
    {
        return rhs switch
        {
            ExprRhsNode e => CheckExpr(e.Value, env),
            
            ReceiveRhsNode r => CheckReceiveRhs(r, env),

            SpawnRhsNode s => CheckSpawnRhs(s, env),

            CallRhsNode c => CheckCallRhs(c, env),

            _ => throw new TypeCheckException($"Unsupported RHS node: {rhs.GetType().Name}")
        };
    }
    private TypeNode CheckReceiveRhs(ReceiveRhsNode rhs, Dictionary<string, BindingType> env)
    {
        var sourceType = CheckExpr(rhs.Source, env);
        if (sourceType != TypeNode.Pid)
        {
            throw new TypeCheckException($"Receive source must be of type Pid, but got {sourceType}.");
        }
        return TypeNode.Int; 
    }
    private TypeNode CheckSpawnRhs(SpawnRhsNode rhs, Dictionary<string, BindingType> env)
    {
        var fn = LookupFunction(rhs.Callee, env);
        if(rhs.Arguments.Count != fn.ParameterTypes.Count)
        {
            throw new TypeCheckException($"Function '{rhs.Callee}' expects {fn.ParameterTypes.Count} arguments, but got {rhs.Arguments.Count}.");
        }
        for(int i = 0; i < rhs.Arguments.Count; i++)
        {
            var actual = CheckExpr(rhs.Arguments[i], env);
            var expected = fn.ParameterTypes[i];
            if(actual != expected)
            {
                throw new TypeCheckException($"Argument {i + 1} of spawn {rhs.Callee}(...) has type {actual}, expected {expected}.");
            }
        }
        return TypeNode.Pid;
    }
    private TypeNode CheckCallRhs(CallRhsNode rhs, Dictionary<string, BindingType> env)
    {
        var fn = LookupFunction(rhs.Callee, env);
        if(rhs.Arguments.Count != fn.ParameterTypes.Count)
        {
            throw new TypeCheckException($"Function '{rhs.Callee}' expects {fn.ParameterTypes.Count} arguments, but got {rhs.Arguments.Count}.");
        }
        for(int i = 0; i < rhs.Arguments.Count; i++)
        {
            var actual = CheckExpr(rhs.Arguments[i], env);
            var expected = fn.ParameterTypes[i];
            if(actual != expected)
            {
                throw new TypeCheckException($"Argument {i + 1} of call {rhs.Callee}(...) has type {actual}, expected {expected}.");
            }
        }
        return fn.ReturnType;
    }
    private FunctionBinding LookupFunction(string name, Dictionary<string, BindingType> env)
    {
        if(!env.TryGetValue(name, out var binding))
        {
            throw new TypeCheckException($"Undefined function: {name}");
        }
        return binding switch
        {
            FunctionBinding fb => fb,
            ValueBinding => throw new TypeCheckException($"Identifier '{name}' is a variable, not a function."),
            _ => throw new TypeCheckException($"Invalid binding type for identifier '{name}'.")
        };
    }
    private StatementCheckResult CheckStatementSequence(IReadOnlyList<StatementNode> statements, Dictionary<string, BindingType> env, TypeNode expectedReturnType)
    {
        var currentEnv = new Dictionary<string, BindingType>(env);
        var definitelyReturns = false;

        foreach (var stmt in statements)
        {
            var result = CheckStatement(stmt, currentEnv, expectedReturnType);

            currentEnv = result.Env;

            if (result.DefinitelyReturns)
            {
                definitelyReturns = true;
                break; // No need to check further statements after a return
            }
        }
        return new StatementCheckResult(currentEnv, definitelyReturns);
    }
    private StatementCheckResult CheckStatement(StatementNode statement, Dictionary<string, BindingType> env, TypeNode expectedReturnType)
    {
        switch (statement)
        {
            case AssignNode a:
                {
                    var lhsType = LookupVariable(a.Name.Name, env);
                    var rhsType = CheckRhs(a.Value, env);
                    if(lhsType != rhsType)
                    {
                        throw new TypeCheckException($"Cannot assign value of type {rhsType} to variable '{a.Name.Name}' of type {lhsType}.");
                    }
                    return new StatementCheckResult(env, false);
                }
            case DeclNode d:
                {
                    if (env.ContainsKey(d.Name))
                    {
                        throw new TypeCheckException($"Variable '{d.Name}' is already declared in this scope.");
                    }

                    var rhs = CheckRhs(d.Value, env);
                    if(rhs != d.DeclType)
                    {
                        throw new TypeCheckException($"Cannot initialize variable '{d.Name}' of type {d.DeclType} with value of type {rhs}.");
                    }
                    var extendedEnv = new Dictionary<string, BindingType>(env)
                    {
                        [d.Name] = new ValueBinding(d.DeclType)
                    };
                    return new StatementCheckResult(extendedEnv, false);
                }
            case PrintNode p:
                {
                    _ = CheckExpr(p.Value, env);
                    return new StatementCheckResult(env, false);
                }
            case ReturnNode r:
                {
                    var actual = CheckExpr(r.Value, env);
                    if(actual != expectedReturnType)
                    {
                        throw new TypeCheckException($"Return type mismatch: expected {expectedReturnType}, but got {actual}.");
                    }
                    return new StatementCheckResult(env, true);
                }
            case SendNode s:
                {
                    var messageType = CheckExpr(s.Message, env);
                    var targetType = CheckExpr(s.Target, env);

                    if(messageType != TypeNode.Int)
                    {
                        throw new TypeCheckException($"Send message must be of type Int, but got {messageType}.");
                    }
                    if(targetType != TypeNode.Pid)
                    {
                        throw new TypeCheckException($"Send target must be of type Pid, but got {targetType}.");
                    }
                    return new StatementCheckResult(env, false);
                }
            case SkipNode:
                return new StatementCheckResult(env, false);
            
            case WhileNode w:
                {
                    var conditionType = CheckExpr(w.Condition, env);
                    if(conditionType != TypeNode.Bool)
                    {
                        throw new TypeCheckException($"While loop condition must be of type Bool, but got {conditionType}.");
                    }
                    CheckStatementSequence(w.Body, new Dictionary<string, BindingType>(env), expectedReturnType);
                    return new StatementCheckResult(env, false);
                }
            case IfNode i:
                {
                    var conditionType = CheckExpr(i.Condition, env);
                    if(conditionType != TypeNode.Bool)
                    {
                        throw new TypeCheckException($"If statement condition must be of type Bool, but got {conditionType}.");
                    }
                    var thenResult = CheckStatementSequence(i.ThenBranch, new Dictionary<string, BindingType>(env), expectedReturnType);
                    var elseResult = CheckStatementSequence(i.ElseBranch, new Dictionary<string, BindingType>(env), expectedReturnType);
                    return new StatementCheckResult(env, thenResult.DefinitelyReturns && elseResult.DefinitelyReturns);
                }
            default:
                throw new TypeCheckException($"Unsupported statement node: {statement.GetType().Name}");
        }
        ;
    }
}