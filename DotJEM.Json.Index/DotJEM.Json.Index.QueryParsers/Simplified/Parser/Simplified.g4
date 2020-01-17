grammar Simplified;

/* Inspired by: https://github.com/lrowe/lucenequery and JIRA */

/*
 * Parser Rules
 */

query  : WS? clause = defaultClause  (WS order = orderingClause)? WS? EOF;

/*
 This implements all clauses grouped into batches of the same type.
 The order implements precedence (important).
*/

defaultClause : orClause (WS? orClause)*;
orClause      : andClause (orOperator andClause)*;
andClause     : notClause (andOperator notClause)*;
notClause     : basicClause (notOperator basicClause)*;
basicClause   :
  WS? LPA defaultClause WS? RPA
  | WS? atom
  ;

atom : value | field | inClause | notInClause;

inClause       : TERM WS IN WS? LPA WS? value ( WS? COMMA WS? value )* WS? RPA;
notInClause    : TERM WS NOT WS IN WS? LPA WS? value ( WS? COMMA WS? value )* WS? RPA;
orderingClause : WS? ORDER WS BY WS orderingField ( WS? COMMA WS? orderingField )* WS?;
orderingField  : WS? TERM (WS (ASC | DESC))?;

field       : TERM WS? operator WS? value;

value       : TERM                                  #Term
            | WILDCARD_TERM                         #Wildcard
            | INTEGER                               #IntegerNumber
            | DECIMAL                               #DecimalNumber
            | PHRASE                                #Phrase
            | STAR                                  #MatchAll
			| DATE                                #Date
			//| TIME                                #Time
			| DATE_TIME                            #DateTime
			| DATE_OFFSET                         #DateOffset
            ;

andOperator : WS? AND;
orOperator  : WS? OR;
notOperator : WS? (AND WS)? NOT;

operator : EQ       #Equals
         | COLON    #Equals
		 | NEQ		#NotEquals
         | GT	    #GreaterThan
		 | GTE      #GreaterThanOrEquals
		 | LT       #LessThan
		 | LTE      #LessThanOrEquals
		 | SIM      #Similar
		 | NSIM     #NotSimilar
		 ;



/*
 * Lexer Rules
 */
 
LPA   : '(';
RPA   : ')';
STAR  : '*';
QMARK : '?';
COMMA : ',';
PLUS  : '+';
MINUS : '-';
DOT   : '.';
COLON : ':';

AND        : A N D      ;
OR         : O R        ;
NOT        : N O T      ;
IN         : I N        ;
ORDER      : O R D E R  ;
BY		   : B Y        ;
ASC        : A S C      ;
DESC       : D E S C    ;

EQ   : '='       ;
NEQ  : '!='      ;
GT   : '>'       ;
GTE  : '>='      ;
LT   : '<'       ;
LTE  : '<='      ;
SIM  : '~'       ;
NSIM : '!~'      ;

WS  : (' '|'\t'|'\r'|'\n'|'\u3000')+;

fragment INT        : '0' .. '9';
fragment ESC        : '\\' .;

INTEGER  : MINUS? INT+;
DECIMAL  : MINUS? INT+ ('.' INT+)?;

// Special Date Handling:
//updated > 2018-03-04T14:41:23+00:00
fragment TIMEOFFSET  : ( MINUS | PLUS ) INT INT ( ':' INT INT );
TIME        : INT INT ':' INT INT ( ':' INT INT )? TIMEOFFSET?;
DATE        : INT INT INT INT MINUS INT INT MINUS INT INT;
DATE_TIME   : DATE 'T' TIME;

// Special Timespan Handling:
fragment TIME_IDEN_CHAR : [a-zA-Z];
fragment NOW         : N O W;
fragment TODAY       : T O D A Y;
fragment SIMPLE_TIMESPAN       : (INT+ '.')? INT INT ':' INT INT ( ':' INT INT ('.' INT INT))?;
fragment COMPLEX_TIMESPAN_PART : INT+ WS? TIME_IDEN_CHAR+;
fragment COMPLEX_TIMESPAN      : (COMPLEX_TIMESPAN_PART WS?)+;
fragment TIME_SPAN             : SIMPLE_TIMESPAN | COMPLEX_TIMESPAN;
DATE_OFFSET           : (NOW | TODAY)? WS? (PLUS|MINUS) WS? TIME_SPAN;


fragment TERM_CHAR  : (~( ' ' | '\t' | '\n' | '\r' | '\u3000' | '\'' | '\"' 
                        | '(' | ')'  | '['  | ']'  | '{'      | '}'  
						| '!' | ':'  | '~'  | '>'  | '='      | '<'
						| '?' | '*'
				        | '\\'| ',' )| ESC );

fragment WILDCARD_CHAR : (~( ' ' | '\t' | '\n' | '\r' | '\u3000' | '\'' | '\"' 
                        | '(' | ')'  | '['  | ']'  | '{'      | '}' 
						| '!' | ':'  | '~'  | '>'  | '='      | '<'
				        | '\\'| ',' )| ESC ); 


TERM   : TERM_CHAR+ ;
WILDCARD_TERM  : WILDCARD_CHAR+;

PHRASE : '\"' ( ESC | ~('\"'|'\\'))+ '\"';

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