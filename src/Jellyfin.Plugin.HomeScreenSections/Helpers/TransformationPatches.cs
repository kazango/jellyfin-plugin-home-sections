using System.Reflection;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.HomeScreenSections.Model;

namespace Jellyfin.Plugin.HomeScreenSections.Helpers
{
    public static class TransformationPatches
    {
        public static string LoadSections(PatchRequestPayload content)
        {
            // replace `",loadSections:` with itself followed by our function followed by `",originalLoadSections:`
            Stream replacementStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(HomeScreenSectionsPlugin).Namespace}.Controllers.loadSections.js")!;
            using TextReader replacementTextReader = new StreamReader(replacementStream);
        
            string[] parts = content.Contents!.Split(",loadSections:", StringSplitOptions.RemoveEmptyEntries);
            Regex variableFind = new Regex(@"var\s+([a-zA-Z][^=]*)=");
            string thisVariableName = variableFind.Matches(parts[0]).Last().Groups[1].Value;
            string replacementText = replacementTextReader.ReadToEnd()
                .Replace("{{this_hook}}", thisVariableName)
                .Replace("{{layoutmanager_hook}}", "n") // TODO: lookup the first "assigned" variable after `var`
                .Replace("{{cardbuilder_hook}}", "h"); // TODO: lookup the last "assigned" variable in block that includes "SmallLibraryTiles" 

            string regex = content.Contents.Replace(",loadSections:", $",loadSections:{replacementText},originalLoadSections:");

            return regex;
        }
    }
}