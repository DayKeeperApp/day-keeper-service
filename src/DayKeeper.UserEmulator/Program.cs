using DayKeeper.UserEmulator;
using DayKeeper.UserEmulator.Configuration;
using DayKeeper.UserEmulator.Orchestration;
using Spectre.Console.Cli;

var app = new CommandApp<RunCommand>();
return await app.RunAsync(args).ConfigureAwait(false);
