using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace Stowage.Terminal.Commands {

    class S3CommandSettings : CommandSettings {
        [CommandOption("-p|--profile")]
        public string? Profile { get; set; }

        [CommandOption("-b|--bucket")]
        public string? Bucket { get; set; }

        [CommandOption("-r|--region")]
        public string? Region { get; set; }
    }

    class S3Command : Command<S3CommandSettings> {
        public override int Execute(CommandContext context, S3CommandSettings settings) {

            Program.Fs = Files.Of.AmazonS3(settings.Bucket, settings.Region, settings.Profile);

            return 0;
        }
    }
}
