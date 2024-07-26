using static TightWiki.EngineFunction.FunctionPrototypeCollection;

namespace TightWiki.EngineFunction
{
    public static class StandardFunctionPrototypes
    {
        private static FunctionPrototypeCollection? _collection;

        public static FunctionPrototypeCollection Collection
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Standard);

                    //Standard functions:
                    _collection.Add("##Snippet: <string>[name]");
                    _collection.Add("##Seq: <string>{key}='Default'");
                    _collection.Add("##Set: <string>[key] | <string>[value]");
                    _collection.Add("##Get: <string>[key]");
                    _collection.Add("##Color: <string>[color] | <string>[text]");
                    _collection.Add("##Tag: <string:infinite>[pageTags]");
                    _collection.Add("##SearchList: <string>[searchPhrase] | <string>{styleName(List,Full)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <bool>{allowFuzzyMatching}='false' | <bool>{showNamespace}='false'");
                    _collection.Add("##TagList: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##NamespaceGlossary: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##NamespaceList: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##TagGlossary: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##RecentlyModified: <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##TextGlossary: <string>[searchPhrase] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##Image: <string>[name] | <integer>{scale}='100' | <string>{altText}=''");
                    _collection.Add("##File: <string>[name] | <string>{linkText} | <bool>{showSize}='false'");
                    _collection.Add("##Related: <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _collection.Add("##Similar: <integer>{similarity}='80' | <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _collection.Add("##EditLink: <string>{linkText}='edit'");
                    _collection.Add("##Inject: <string>[pageName]");
                    _collection.Add("##Include: <string>[pageName]");
                    _collection.Add("##BR: <integer>{Count}='1'");
                    _collection.Add("##HR: <integer>{Height}='1'");
                    _collection.Add("##Revisions:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _collection.Add("##Attachments:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _collection.Add("##Title:");
                    _collection.Add("##Navigation:");
                    _collection.Add("##Name:");
                    _collection.Add("##Namespace:");
                    _collection.Add("##Created:");
                    _collection.Add("##LastModified:");
                    _collection.Add("##AppVersion:");

                    _collection.Add("##ProfileGlossary: <integer>{Top}='1000' | <integer>{pageSize}='100' | <string>{searchToken}=''");
                    _collection.Add("##ProfileList: <integer>{Top}='1000' | <integer>{pageSize}='100' | <string>{searchToken}=''");
                }

                return _collection;
            }
        }
    }
}
