
// Expressions
[assembly: ASTNode("Expression", "Binary", "Expression left, Token operation, Expression right")]
[assembly: ASTNode("Expression", "Grouping", "Expression expr")]
[assembly: ASTNode("Expression", "Literal", "Object value")]
[assembly: ASTNode("Expression", "Unary", "Token operation, Expression right")]
[assembly: ASTNode("Expression", "Variable", "Token name")]
[assembly: ASTNode("Expression", "Assignment", "Token name, Expression value")]
[assembly: ASTNode("Expression", "Logical", "Expression left, Token operation, Expression right")]
[assembly: ASTNode("Expression", "Call", "Expression callee, Token paren, List<Expression> arguments")]
[assembly: ASTNode("Expression", "Get", "Expression obj, Token name")]
[assembly: ASTNode("Expression", "Set", "Expression obj, Token name, Expression value")]
[assembly: ASTNode("Expression", "This", "Token keyword")]
[assembly: ASTNode("Expression", "Super", "Token keyword, Token method")]


// Statements
[assembly: ASTNode("Statement", "Expression", "Silver.Expression expr")]
[assembly: ASTNode("Statement", "Print", "Silver.Expression expr")]
[assembly: ASTNode("Statement", "Var", "Token name, Silver.Expression initializer")]
[assembly: ASTNode("Statement", "Block", "List<Statement> statements")]
[assembly: ASTNode("Statement", "If", "Silver.Expression condition, Statement thenBranch, Statement elseBranch")]
[assembly: ASTNode("Statement", "While", "Silver.Expression condition, Statement body")]
[assembly: ASTNode("Statement", "Function", "Token name, List<Token> parameters, List<Statement> body")]
[assembly: ASTNode("Statement", "Return", "Token keyword, Silver.Expression value")]
[assembly: ASTNode("Statement", "Class", "Token name, Silver.Expression.Variable superclass, List<Statement.Function> methods")]

