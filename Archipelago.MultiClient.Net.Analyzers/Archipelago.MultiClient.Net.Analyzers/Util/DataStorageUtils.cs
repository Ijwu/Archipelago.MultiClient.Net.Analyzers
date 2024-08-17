using Microsoft.CodeAnalysis;

namespace Archipelago.MultiClient.Net.Analyzers.Util
{
    internal class DataStorageUtils
    {
        public static bool IsTypeDataStorageElement(ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? dataStorageElement = compilation.GetTypeByMetadataName("Archipelago.MultiClient.Net.Models.DataStorageElement");
            return type != null && dataStorageElement != null && dataStorageElement.Equals(type, SymbolEqualityComparer.Default);
        }

        public static bool IsTypeDataStorageHelper(ITypeSymbol? type, Compilation compilation)
        {
            INamedTypeSymbol? iDataStorageHelper = compilation.GetTypeByMetadataName("Archipelago.MultiClient.Net.Helpers.IDataStorageHelper");
            return type != null && iDataStorageHelper != null && (
                iDataStorageHelper.Equals(type, SymbolEqualityComparer.Default)
                || type.AllInterfaces.Contains(iDataStorageHelper, SymbolEqualityComparer.Default)
            );
        }
    }
}
