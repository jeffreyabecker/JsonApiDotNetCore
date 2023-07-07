@java -classpath C:\tools\antlr-4.13.0-complete.jar org.antlr.v4.Tool -Dlanguage=CSharp -no-visitor -no-listener JadncFilters.g4 -package JsonApiDotNetCore.ExtendedQuery.QueryLanguage
@del *.interp
@del *.tokens

