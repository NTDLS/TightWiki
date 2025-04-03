using NTDLS.SqliteDapperWrapper;

namespace DummyPageGenerator
{
    public static class WordsRepository
    {
        public static SqliteManagedFactory Words { get; private set; } = new();

        public static int GetWordsCount()
            => Words.ExecuteScalar<int>("GetWordsCount.sql");

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

                var word = Words.QueryFirstOrDefault<string>("GetSingleWordAt.sql", param);
                if (word != null)
                {
                    result.Add(word);
                }
            }

            return result;
        }
    }
}
