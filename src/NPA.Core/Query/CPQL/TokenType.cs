namespace NPA.Core.Query.CPQL;

/// <summary>
/// Represents the type of a token in CPQL.
/// </summary>
public enum TokenType
{
    /// <summary>End of input token.</summary>
    Eof,
    
    /// <summary>SELECT keyword.</summary>
    Select,
    /// <summary>FROM keyword.</summary>
    From,
    /// <summary>WHERE keyword.</summary>
    Where,
    /// <summary>ORDER BY keyword.</summary>
    OrderBy,
    /// <summary>GROUP BY keyword.</summary>
    GroupBy,
    /// <summary>HAVING keyword.</summary>
    Having,
    /// <summary>JOIN keyword.</summary>
    Join,
    /// <summary>INNER JOIN keyword.</summary>
    InnerJoin,
    /// <summary>LEFT JOIN keyword.</summary>
    LeftJoin,
    /// <summary>RIGHT JOIN keyword.</summary>
    RightJoin,
    /// <summary>FULL JOIN keyword.</summary>
    FullJoin,
    /// <summary>ON keyword.</summary>
    On,
    /// <summary>AS keyword.</summary>
    As,
    /// <summary>DISTINCT keyword.</summary>
    Distinct,
    /// <summary>UPDATE keyword.</summary>
    Update,
    /// <summary>SET keyword.</summary>
    Set,
    /// <summary>DELETE keyword.</summary>
    Delete,
    /// <summary>INSERT keyword.</summary>
    Insert,
    /// <summary>INTO keyword.</summary>
    Into,
    /// <summary>VALUES keyword.</summary>
    Values,
    
    /// <summary>AND logical operator.</summary>
    And,
    /// <summary>OR logical operator.</summary>
    Or,
    /// <summary>NOT logical operator.</summary>
    Not,
    
    /// <summary>Equal comparison operator (=).</summary>
    Equal,
    /// <summary>Not equal comparison operator (&lt;&gt; or !=).</summary>
    NotEqual,
    /// <summary>Less than comparison operator (&lt;).</summary>
    LessThan,
    /// <summary>Less than or equal comparison operator (&lt;=).</summary>
    LessThanOrEqual,
    /// <summary>Greater than comparison operator (&gt;).</summary>
    GreaterThan,
    /// <summary>Greater than or equal comparison operator (&gt;=).</summary>
    GreaterThanOrEqual,
    /// <summary>LIKE comparison operator.</summary>
    Like,
    /// <summary>IN comparison operator.</summary>
    In,
    /// <summary>BETWEEN comparison operator.</summary>
    Between,
    /// <summary>IS comparison operator.</summary>
    Is,
    /// <summary>NULL keyword.</summary>
    Null,
    
    /// <summary>Addition operator (+).</summary>
    Plus,
    /// <summary>Subtraction operator (-).</summary>
    Minus,
    /// <summary>Multiplication operator (*).</summary>
    Multiply,
    /// <summary>Division operator (/).</summary>
    Divide,
    /// <summary>Modulo operator (%).</summary>
    Modulo,
    
    /// <summary>COUNT aggregate function.</summary>
    Count,
    /// <summary>SUM aggregate function.</summary>
    Sum,
    /// <summary>AVG aggregate function.</summary>
    Avg,
    /// <summary>MIN aggregate function.</summary>
    Min,
    /// <summary>MAX aggregate function.</summary>
    Max,
    
    /// <summary>UPPER string function.</summary>
    Upper,
    /// <summary>LOWER string function.</summary>
    Lower,
    /// <summary>LENGTH string function.</summary>
    Length,
    /// <summary>SUBSTRING string function.</summary>
    Substring,
    /// <summary>TRIM string function.</summary>
    Trim,
    /// <summary>CONCAT string function.</summary>
    Concat,
    
    /// <summary>YEAR date function.</summary>
    Year,
    /// <summary>MONTH date function.</summary>
    Month,
    /// <summary>DAY date function.</summary>
    Day,
    /// <summary>HOUR date function.</summary>
    Hour,
    /// <summary>MINUTE date function.</summary>
    Minute,
    /// <summary>SECOND date function.</summary>
    Second,
    /// <summary>NOW date function.</summary>
    Now,
    
    /// <summary>Left parenthesis (.</summary>
    LeftParenthesis,
    /// <summary>Right parenthesis ).</summary>
    RightParenthesis,
    /// <summary>Comma (,).</summary>
    Comma,
    /// <summary>Dot (.).</summary>
    Dot,
    /// <summary>Semicolon (;).</summary>
    Semicolon,
    /// <summary>Colon (:).</summary>
    Colon,
    
    /// <summary>Identifier token (entity, property, function names).</summary>
    Identifier,
    /// <summary>String literal token.</summary>
    StringLiteral,
    /// <summary>Number literal token.</summary>
    NumberLiteral,
    /// <summary>Boolean literal token (TRUE, FALSE).</summary>
    BooleanLiteral,
    
    /// <summary>Parameter token (:paramName).</summary>
    Parameter,
    
    /// <summary>ASC sorting direction.</summary>
    Asc,
    /// <summary>DESC sorting direction.</summary>
    Desc
}

