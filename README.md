Implementations of the Lox programming language from https://craftinginterpreters.com

This repository contains 2 implementations:
- Sharplox - C# tree walker implementation, close translation of the jlox implementation from the book with some added functionality (anonymous functions and break/continue statements)

  Note: the performance of this implementation is very poor when involving `return/break/continue` statements, as it relies on throwing exceptions. Jlox disables collecting stack traces for these exceptions, which makes them significantly less expensive, but this is not possible with C# exceptions.
- loxide - WIP Rust bytecode VM implementation
