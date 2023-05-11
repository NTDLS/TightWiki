namespace TightWiki.Shared.Wiki.Function
{
    public static class FunctionPrototypeDefinitions
    {
        private static FunctionPrototypeCollection _functionPrototypes;

        public static FunctionPrototype Get(string functionPrefix, string functionName)
        {
            return Collection.Get(functionPrefix, functionName);
        }

        public static FunctionPrototypeCollection Collection
        {
            get
            {
                if (_functionPrototypes == null)
                {
                    _functionPrototypes = new FunctionPrototypeCollection();

                    //Scope functions:
                    _functionPrototypes.Add("$$Code: <string>{language(auto,wiki,cpp,lua,graphql,swift,r,yaml,kotlin,scss,shell,vbnet,json,objectivec,perl,diff,wasm,php,xml,bash,csharp,css,go,ini,javascript,less,makefile,markdown,plaintext,python,python-repl,ruby,rust,sql,typescript)}='auto'");
                    _functionPrototypes.Add("$$Bullets: <string>{type(unordered,ordered)}='unordered'");
                    _functionPrototypes.Add("$$Order: <string>{direction(ascending,descending)}='ascending'");
                    _functionPrototypes.Add("$$Jumbotron:");
                    _functionPrototypes.Add("$$Callout: <string>{styleName(default,primary,secondary,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _functionPrototypes.Add("$$Background: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _functionPrototypes.Add("$$Foreground: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _functionPrototypes.Add("$$Alert: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _functionPrototypes.Add("$$Card: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _functionPrototypes.Add("$$Collapse: <string>{linkText}='Show'");
                    _functionPrototypes.Add("$$Table: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _functionPrototypes.Add("$$StripedTable: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _functionPrototypes.Add("$$DefineSnippet: <string>[name]");

                    //Standard functions:
                    _functionPrototypes.Add("##Snippet: <string>[name]");
                    _functionPrototypes.Add("##Seq: <string>{key}='Default'");
                    _functionPrototypes.Add("##Set: <string>[key] | <string>[value]");
                    _functionPrototypes.Add("##Get: <string>[key]");
                    _functionPrototypes.Add("##Color: <string>[color] | <string>[text]");
                    _functionPrototypes.Add("##Tag: <string:infinite>[pageTags]");
                    _functionPrototypes.Add("##SearchList: <string>[searchPhrase] | <string>{styleName(List,Full)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <bool>{allowFuzzyMatching}='false' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##TagList: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##SearchCloud: <string>[searchPhrase] | <integer>{Top}='1000'");
                    _functionPrototypes.Add("##NamespaceGlossary: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##NamespaceList: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##TagGlossary: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##RecentlyModified: <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##TextGlossary: <string>[searchPhrase] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _functionPrototypes.Add("##TagCloud: <string>[pageTag] | <integer>{Top}='1000'");
                    _functionPrototypes.Add("##Image: <string>[name] | <integer>{scale}='100' | <string>{altText}=''");
                    _functionPrototypes.Add("##File: <string>[name] | <string>{linkText} | <bool>{showSize}='false'");
                    _functionPrototypes.Add("##Related: <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _functionPrototypes.Add("##Similar: <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _functionPrototypes.Add("##Tags: <string>{styleName(Flat,List)}='List'");
                    _functionPrototypes.Add("##EditLink: <string>{linkText}='edit'");
                    _functionPrototypes.Add("##Inject: <string>[pageName]");
                    _functionPrototypes.Add("##Include: <string>[pageName]");
                    _functionPrototypes.Add("##BR: <integer>{Count}='1'");
                    _functionPrototypes.Add("##HR: <integer>{Height}='1'");
                    _functionPrototypes.Add("##History:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _functionPrototypes.Add("##Attachments:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _functionPrototypes.Add("##TOC:<bool>{alphabetized}='false'");
                    _functionPrototypes.Add("##Title:");
                    _functionPrototypes.Add("##Navigation:");
                    _functionPrototypes.Add("##Name:");
                    _functionPrototypes.Add("##Namespace:");
                    _functionPrototypes.Add("##Created:");
                    _functionPrototypes.Add("##LastModified:");
                    _functionPrototypes.Add("##AppVersion:");

                    //Processing instructions:
                    _functionPrototypes.Add("@@Deprecate:");
                    _functionPrototypes.Add("@@Protect:<bool>{isSilent}='false'");
                    _functionPrototypes.Add("@@Template:");
                    _functionPrototypes.Add("@@Review:");
                    _functionPrototypes.Add("@@NoCache:");
                    _functionPrototypes.Add("@@Include:");
                    _functionPrototypes.Add("@@Draft:");
                }

                return _functionPrototypes;
            }
        }
    }
}
