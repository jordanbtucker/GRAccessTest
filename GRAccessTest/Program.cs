using System;
using ArchestrA.GRAccess;

namespace GRAccessTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use GRAccessApp to instantiate a GRAccess instance.
            var gra = new GRAccessApp();

            // Query a list of galaxies on the current computer. Supply a GRNodeName
            // argument if this app is not running on the GR.
            var galaxies = gra.QueryGalaxies();

            // GRAccess methods typically do not throw, so we need to check the
            // `CommandResult` property to see if the last method call was successful.
            AssertSuccess(gra.CommandResult);

            // Collections are 1-based indexed.
            var galaxy = galaxies[1];
            if (galaxy is null)
                throw new Exception("A galaxy could not be found");

            Console.WriteLine($"Found galaxy \"{galaxy.Name}\"");

            // Must login to the galaxy even if security is not enabled.
            galaxy.Login();
            AssertSuccess(galaxy.CommandResult);

            // Ensure that an AppEngine, MainArea, and MainTank instance each exist using a generic helper.
            Console.WriteLine("Ensuring that basic instances exist");
            var grPlatform = EnsureInstance(galaxy, "GRPlatform", "$WinPlatform");
            var appEngine = EnsureInstance(galaxy, "AppEngine", "$AppEngine", grPlatform);
            var mainArea = EnsureInstance(galaxy, "MainArea", "$Area", appEngine);
            var mainTank = EnsureInstance(galaxy, "MainTank", "$UserDefined", mainArea);

            // Get a list of objects to export, and export those to an aaPKG file.
            Console.WriteLine("Exporting a set of objects to an aaPKG file");
            var toExport = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, ["GRPlatform", "AppEngine", "MainTank"]);
            toExport.ExportObjects(EExportType.exportAsPDF, @"C:\Users\Jordan\Desktop\Objects.aaPKG");
        }

        /// <summary>
        /// Tests whether a GRPlatform instance exists, and if not, creates a new
        /// instance.
        /// </summary>
        /// <param name="galaxy">The galaxy to ensure a GRPlatform exists in.</param>
        static IInstance EnsureGRPlatform(IGalaxy galaxy)
        {
            var instanceName = "GRPlatform";
            var templateName = "$WinPlatform";

            // Only a list of instances or templates can be queried at a time. Here we
            // request one instance with a specific name. We'll get a list of either
            // zero or one instances.
            var instances = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, [instanceName]);
            AssertSuccess(galaxy.CommandResult);

            IInstance? grPlatform = instances[instanceName] as IInstance;
            if (grPlatform is null)
            {
                // Only instances or templates can be queried at a time. Use `namedLike`
                // and "%" to query all templates or instances.
                var templates = galaxy.QueryObjects(EgObjectIsTemplateOrInstance.gObjectIsTemplate, EConditionType.namedLike, "%");
                AssertSuccess(galaxy.CommandResult);

                // Indexes can be 1-based numbers or strings.
                ITemplate? winPlatform = templates[templateName] as ITemplate;
                if (winPlatform is null)
                    throw new Exception($"The {templateName} template could not be found");

                // Create a new instance of a $WinPlatform named GRPlatform.
                grPlatform = winPlatform.CreateInstance(instanceName);
                AssertSuccess(winPlatform.CommandResult);
            }

            return grPlatform;
        }

        /// <summary>
        /// Tests whether a GRPlatform isntance exists, and if not, creates a
        /// new instance from the given template, optionally assigning it to 
        /// the given host or area.
        /// </summary>
        /// <param name="galaxy">The galaxy to ensure the instance exists in.</param>
        /// <param name="instanceName">The name of the instance.</param>
        /// <param name="templateName">The template to derive the instance from, if it doesn't exist.</param>
        /// <param name="parent">The optional host or are to assign the instance to.</param>
        /// <returns>The object instance existed or was created.</returns>
        /// <exception cref="Exception"></exception>
        static IInstance EnsureInstance(IGalaxy galaxy, string instanceName, string templateName, IInstance? parent = null)
        {
            // Only a list of instances or templates can be queried at a time. Here we
            // request one instance with a specific name. We'll get a list of either
            // zero or one instances.
            var instances = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsInstance, [instanceName]);
            AssertSuccess(galaxy.CommandResult);

            // Indexes can be 1-based numbers or strings.
            IInstance? instance = instances[instanceName] as IInstance;
            if (instance is null)
            {
                var templates = galaxy.QueryObjectsByName(EgObjectIsTemplateOrInstance.gObjectIsTemplate, [templateName]);
                AssertSuccess(galaxy.CommandResult);

                ITemplate? template = templates[templateName] as ITemplate;
                if (template is null)
                    throw new Exception($"The {templateName} template could not be found");

                instance = template.CreateInstance(instanceName);
                AssertSuccess(template.CommandResult);
            }

            if (parent is not null)
            {
                // Use the `basedOn` property to determine the base template of
                // the parent to check whether it's an area.
                if (parent.basedOn == "$Area")
                    instance.Area = parent.Tagname;
                else
                    instance.Host = parent.Tagname;
            }

            return instance;
        }

        /// <summary>
        /// Asserts that the given command result is successful. If the command result
        /// is null or successful, then no exception is thrown. Otherwise, an
        /// exception is thrown.
        /// </summary>
        /// <param name="commandResult">The command result to test.</param>
        /// <exception cref="Exception">
        /// Thrown if the command result is not null and is not successful.
        /// </exception>
        static void AssertSuccess(ICommandResult commandResult)
        {
            // Command results have a Text and CustomMessage property, but I
            // don't know which ones are ever set, so I just return both in the
            // error.
            if (commandResult is not null && !commandResult.Successful)
                throw new Exception($"{commandResult.Text}, {commandResult.CustomMessage}");

            // Optionally, use the command result's ID to determine the error message.
            //if (commandResult is not null && !commandResult.Successful)
            //    if (commandResult.ID == EGRCommandResult.cmdCouldntCreateFile)
            //        throw new Exception("Could not create file");
            //    else if (commandResult.ID == EGRCommandResult.cmdInsufficientPermissions)
            //        throw new Exception("Insufficient privileges");
            //    ...
        }
    }
}
