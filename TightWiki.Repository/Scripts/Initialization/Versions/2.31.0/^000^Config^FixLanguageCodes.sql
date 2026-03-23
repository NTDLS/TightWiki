UPDATE
    ConfigurationEntry
SET
    Value = CASE Value
        WHEN 'English' THEN 'en'
        WHEN 'Arabic' THEN 'ar'
        WHEN 'Azerbaijani' THEN 'az'
        WHEN 'Belarusian' THEN 'be'
        WHEN 'Bulgarian' THEN 'bg'
        WHEN 'Czech' THEN 'cs'
        WHEN 'Danish' THEN 'da'
        WHEN 'German' THEN 'de'
        WHEN 'Greek' THEN 'el'
        WHEN 'Spanish' THEN 'es'
        WHEN 'Estonian' THEN 'et'
        WHEN 'Persian' THEN 'fa'
        WHEN 'Finnish' THEN 'fi'
        WHEN 'French' THEN 'fr'
        WHEN 'Hebrew' THEN 'he'
        WHEN 'Croatian' THEN 'hr'
        WHEN 'Hungarian' THEN 'hu'
        WHEN 'Indonesian' THEN 'id'
        WHEN 'Icelandic' THEN 'is'
        WHEN 'Italian' THEN 'it'
        WHEN 'Japanese' THEN 'ja'
        WHEN 'Georgian' THEN 'ka'
        WHEN 'Kazakh' THEN 'kk'
        WHEN 'Korean' THEN 'ko'
        WHEN 'Lithuanian' THEN 'lt'
        WHEN 'Latvian' THEN 'lv'
        WHEN 'Malay' THEN 'ms'
        WHEN 'Dutch' THEN 'nl'
        WHEN 'Norwegian' THEN 'nn'
        WHEN 'Bokmål' THEN 'nb'
        WHEN 'Polish' THEN 'pl'
        WHEN 'Portuguese' THEN 'pt'
        WHEN 'Romanian' THEN 'ro'
        WHEN 'Russian' THEN 'ru'
        WHEN 'Slovak' THEN 'sk'
        WHEN 'Slovenian' THEN 'sl'
        WHEN 'Serbian' THEN 'sr'
        WHEN 'Swedish' THEN 'sv'
        WHEN 'Thai' THEN 'th'
        WHEN 'Turkish' THEN 'tr'
        WHEN 'Ukrainian' THEN 'uk'
        WHEN 'Vietnamese' THEN 'vi'
        WHEN 'Chinese simplified' THEN 'zh-Hans'
        WHEN 'Chinese traditional' THEN 'zh-Hant'
        WHEN 'Bengali' THEN 'bn'
        WHEN 'Hindi' THEN 'hi'
        WHEN 'Urdu' THEN 'ur'
        ELSE Value
    END
WHERE
	Name = 'Default Language'
	AND Value IN (
        'English','Arabic','Azerbaijani','Belarusian','Bulgarian','Czech','Danish',
        'German','Greek','Spanish','Estonian','Persian','Finnish','French','Hebrew',
        'Croatian','Hungarian','Indonesian','Icelandic','Italian','Japanese',
        'Georgian','Kazakh','Korean','Lithuanian','Latvian','Malay','Dutch',
        'Norwegian','Bokmål','Polish','Portuguese','Romanian','Russian','Slovak',
        'Slovenian','Serbian','Swedish','Thai','Turkish','Ukrainian','Vietnamese',
        'Chinese simplified','Chinese traditional','Bengali','Hindi','Urdu'
    );
