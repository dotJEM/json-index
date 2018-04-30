grammar Extended;

/* Inspired by: https://github.com/lrowe/lucenequery and JIRA */

/*
 * Parser Rules
 */

query  : sep? clause = defaultClause (sep order = orderingClause)? sep? EOF;
//mainQ :
//  sep? clause=clauseDefault sep? EOF
//  ;

  //m:(a b AND c OR d OR e)
  // without duplicating the rules (but it allows recursion)
defaultClause : orClause (sep? orClause)*;
orClause : clauseAnd (orOperator clauseAnd)*;
clauseAnd : clauseNot (andOperator clauseNot)*;
clauseNot : clauseBasic (notOperator clauseBasic)*;
clauseBasic
  : sep? modifier? LPAREN defaultClause sep? RPAREN term_modifier?
  | sep? atom
  ;

atom
  : modifier? field multi_value term_modifier?
  | modifier? field? value term_modifier?
  ;


joinClause     : TERM_NORMAL sep IN sep? LPAREN sep? defaultClause sep? RPAREN;
inClause       : TERM_NORMAL sep IN sep? LPAREN sep? value ( sep? COMMA sep? value )* sep? RPAREN;
notInClause    : TERM_NORMAL sep NOT sep IN sep? LPAREN sep? value ( sep? COMMA sep? value )* sep? RPAREN;

orderingClause : sep? ORDER sep BY sep orderingField ( sep? COMMA sep? orderingField )* sep?;
orderingField  : sep? TERM_NORMAL (sep (ASC | DESC))?;

field : TERM_NORMAL COLON sep?;

value
  : range_term
  | normal
  | truncated
  | quoted
  | quoted_truncated
  | QMARK
  | anything
  | STAR
  ;

anything : STAR COLON STAR;

two_sided_range_term :
  start_type=(LBRACK|LCURLY)
  sep?
  min=range_value
  sep?
  (TO|MINUS) 
  sep? 
  max=range_value 
  sep? 
  end_type=(RBRACK|RCURLY)
  ;

one_sided_range_term :
  operator=(GT|GTE|LT|LTE)
  value=range_value
  ;

range_term 
  : two_sided_range_term
  | one_sided_range_term
  ;

range_value :
  truncated
  | quoted
  | quoted_truncated
  | date
  | normal
  | STAR
  ;

multi_value : LPAREN defaultClause sep? RPAREN;

normal
  : TERM_NORMAL
  | INTEGER
  | DECIMAL
  ;

truncated: TERM_TRUNCATED;

quoted_truncated: PHRASE_ANYTHING;

quoted: PHRASE;

modifier: PLUS | MINUS | EXCL;

term_modifier 
  : boost fuzzy?
  | fuzzy boost?
  ;

boost :
  (CARAT) // set the default value
  (POSDECIMAL)? //replace the default with user input
  ;

fuzzy :
  (TILDE) // set the default value
  (POSDECIMAL)? //replace the default with user input
  ;

notOperator  :
  sep? AND sep NOT
  | sep? NOT
  ;

andOperator  :
  sep? AND
  ;

orOperator :  sep? OR
  ;

date  : DATE | DATE_TIME;

/* ================================================================
 * =                     LEXER                                    =
 * ================================================================
 */

LPAREN  : '(';
RPAREN  : ')';
LBRACK  : '[';
RBRACK  : ']';
COLON   : ':';  //this must NOT be fragment
PLUS    : '+';
MINUS   : '-';
EXCL    : '!';
STAR    : '*';
COMMA   : ',';
QMARK   : '?'+;

fragment VBAR  : '|' ;
fragment AMPER : '&' ;

LCURLY  : '{' ;
RCURLY  : '}' ;
CARAT   : '^' (INT+ ('.' INT+)?)?;
TILDE   : '~' (INT+ ('.' INT+)?)?;
DQUOTE  : '\"';
SQUOTE  : '\'';

/* We want to be case insensitive */
TO         : T O        ;
AND        : A N D      ;
OR         : O R        ;
NOT        : N O T      ;
IN         : I N        ;
ORDER      : O R D E R  ;
BY		   : B Y        ;
ASC        : A S C      ;
DESC       : D E S C    ;

GT   : '>'       ;
GTE  : '>='      ;
LT   : '<'       ;
LTE  : '<='      ;

sep : WS+;

WS  : (' '|'\t'|'\r'|'\n'|'\u3000')+;

fragment INT: '0' .. '9';
fragment ESC:  '\\' .;
fragment TERM_START_CHAR :
  (~(' ' | '\t' | '\n' | '\r' | '\u3000'
        | '\'' | '\"'
        | '(' | ')' | '[' | ']' | '{' | '}'
        | '+' | '-' | '!' | ':' | '~' | '^'
        | '?' | '*' | '\\'
        )
   | ESC );

fragment TERM_CHAR : (TERM_START_CHAR | '-' | '+');

INTEGER    : MINUS? INT+;
DECIMAL    : MINUS? INT+ ('.' INT+)?;
POSDECIMAL : INT+ ('.' INT+)?;

// Special Date Handling:
//updated > 2018-03-04T14:41:23+00:00
fragment TIMEOFFSET  : ( MINUS | PLUS ) INT INT ( ':' INT INT );
TIME        : INT INT ':' INT INT ( ':' INT INT )? TIMEOFFSET?;
DATE        : INT INT INT INT MINUS INT INT MINUS INT INT;
DATE_TIME   : DATE 'T' TIME;

// Special Timespan Handling:
fragment TIME_IDEN_CHAR        : [a-zA-Z];
fragment NOW                   : N O W;
fragment TODAY                 : T O D A Y;
fragment SIMPLE_TIMESPAN       : (INT+ '.')? INT INT ':' INT INT ( ':' INT INT ('.' INT INT))?;
fragment COMPLEX_TIMESPAN_PART : INT+ WS? TIME_IDEN_CHAR+;
fragment COMPLEX_TIMESPAN      : (COMPLEX_TIMESPAN_PART WS?)+;
fragment TIME_SPAN             : SIMPLE_TIMESPAN | COMPLEX_TIMESPAN;
DATE_OFFSET                    : (NOW | TODAY)? WS? (PLUS|MINUS) WS? TIME_SPAN;

TERM_NORMAL : TERM_START_CHAR ( TERM_CHAR )* ;
TERM_TRUNCATED :
  (STAR|QMARK) (TERM_CHAR+ (QMARK|STAR))+ (TERM_CHAR)*
  | TERM_START_CHAR (TERM_CHAR* (QMARK|STAR))+ (TERM_CHAR)*
  | (STAR|QMARK) TERM_CHAR+
  ;

PHRASE : DQUOTE (ESC|~('\"'|'\\'|'?'|'*'))+ DQUOTE;
PHRASE_ANYTHING : DQUOTE (ESC|~('\"'|'\\'))+ DQUOTE;

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