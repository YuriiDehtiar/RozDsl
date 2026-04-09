using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace RozDsl.Vsix.LanguageClient
{
    internal static class RozContentTypeDefinitions
    {
        [Export]
        [Name("roz")]
        [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
        internal static ContentTypeDefinition RozContentTypeDefinition;

        [Export]
        [FileExtension(".roz")]
        [ContentType("roz")]
        internal static FileExtensionToContentTypeDefinition RozFileExtensionDefinition;
    }
}