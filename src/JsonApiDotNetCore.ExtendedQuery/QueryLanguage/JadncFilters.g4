grammar JadncFilters;


expr
 : literal_value #literalExpr
 | BIND_PARAMETER #variableExpr
 | K_NOT expr #notExpr
 | expr ( '*' | '/' | '%' ) expr #mulExpr
 | expr ( '+' | '-' ) expr #addExpr
 | expr ( '<' | '<=' | '>' | '>=' ) expr #greaterLessExpr
 | expr ( '=' | '==' | '!=' | '<>'  ) expr #equalExpr
 | expr K_AND  expr #andExpr
 | expr K_OR expr #orExpr
 | function_name OPEN_PAR ( (',')? expr ( ',' expr )* | '*' )? CLOSE_PAR #functionExpr
 | OPEN_PAR expr CLOSE_PAR #nestedExpr
 | expr K_NOT? K_LIKE expr  #likeExpr
 | expr K_IS K_NOT? K_NULL #isNullExpr
 | K_IS K_NOT? K_OF K_TYPE IDENTIFIER #ofTypeExpr
 | K_HAS IDENTIFIER #hasExpression
 | K_IF expr K_THEN expr K_ELSE expr K_END #ifExpr
 | expr K_NOT? K_IN ( OPEN_PAR ( expr ( ',' expr )* )?  CLOSE_PAR ) #inExpr
;

literal_value
 : NUMERIC_LITERAL #numLiteral
 | STRING_LITERAL #strLiteral
 | DATE_LITERAL #dateLiteral
 | (K_TRUE|K_FALSE) #booleanLiteral
 | K_NULL #nullLiteral
 ;
BIND_PARAMETER: '$'  [a-zA-Z0-9_.]+ 
;


function_name
 : IDENTIFIER
 ;

SCOL : ';';
DOT : '.';
OPEN_PAR : '(';
CLOSE_PAR : ')';
COMMA : ',';
ASSIGN : '=';
STAR : '*';
PLUS : '+';
MINUS : '-';
TILDE : '~';
PIPE2 : '||';
DIV : '/';
MOD : '%';
LT2 : '<<';
GT2 : '>>';
AMP : '&';
PIPE : '|';
LT : '<';
LT_EQ : '<=';
GT : '>';
GT_EQ : '>=';
EQ : '==';
HASH: '#';
NOT_EQ1 : '!=';
NOT_EQ2 : '<>';
K_ADD : A D D;
K_AND : A N D;
K_AS : A S;
K_CASE : C A S E;
K_CAST : C A S T;
K_DEFAULT : D E F A U L T;
K_ELSE : E L S E;
K_END : E N D;
K_IF : I F;
K_IN : I N;
K_IS : I S;
K_ISNULL : I S N U L L;
K_LIKE : L I K E;
K_NOT : N O T;
K_NOTNULL : N O T N U L L;
K_NULL : N U L L;
K_OF : O F;
K_OR : O R;
K_REGEXP : R E G E X P;
K_THEN : T H E N;
K_TO : T O;
K_WHEN : W H E N;
K_STRING: S T R I N G;
K_NUMBER: N U M B E R;
K_DATE: D A T E;
K_BOOLEAN: B O O L E A N;
K_TRUE: T R U E;
K_FALSE: F A L S E;
K_YES: Y E S;
K_NO: N O;
K_HAS: H A S;
K_TYPE: T Y P E;

IDENTIFIER
 : [a-zA-Z_] [a-zA-Z_0-9]* // TODO check: needs more chars in set
 ;

NUMERIC_LITERAL
 : MINUS? DIGIT+ ( '.' DIGIT* )? ( E [-+]? DIGIT+ )?
 | '.' DIGIT+ ( E [-+]? DIGIT+ )?
 ;
 BOOLEAN_LITERAL: K_TRUE | K_FALSE ;



STRING_LITERAL
 : '\'' ( ~'\'' | '\'\'' )* '\''
 ;
DATE_LITERAL:
HASH (DIGIT DIGIT DIGIT DIGIT MINUS DIGIT DIGIT MINUS DIGIT DIGIT (' ' DIGIT DIGIT':' DIGIT DIGIT)?) HASH;

SPACES
 : [ \u000B\t\r\n] -> channel(HIDDEN)
 ;

UNEXPECTED_CHAR
 : .
 ;

fragment DIGIT : [0-9];
fragment LETTER:[a-zA-Z_];
fragment A : [aA];
fragment B : [bB];
fragment C : [cC];
fragment D : [dD];
fragment E : [eE];
fragment F : [fF];
fragment G : [gG];
fragment H : [hH];
fragment I : [iI];
fragment J : [jJ];
fragment K : [kK];
fragment L : [lL];
fragment M : [mM];
fragment N : [nN];
fragment O : [oO];
fragment P : [pP];
fragment Q : [qQ];
fragment R : [rR];
fragment S : [sS];
fragment T : [tT];
fragment U : [uU];
fragment V : [vV];
fragment W : [wW];
fragment X : [xX];
fragment Y : [yY];
fragment Z : [zZ];
