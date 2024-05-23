using TightWiki.DataStorage;

namespace TightWiki.Repository
{
    public static class WordsRepository
    {
        public static int GetWordsCount()
            => ManagedDataStorage.Words.ExecuteScalar<int>("GetWordsCount");

        public static List<string> GetRandomWords(int count)
        {
            var result = new List<string>();

            var random = new Random();
            int countOfWords = GetWordsCount();

            while (result.Count < count)
            {
                var param = new
                {
                    Offset = random.Next(countOfWords),
                };

                var word = ManagedDataStorage.Words.QueryFirstOrDefault<string>("GetSingleWordAt", param);
                if (word != null)
                {
                    result.Add(word);
                }
            }

            return result;
        }
    }
}
