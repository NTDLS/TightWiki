UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cerulean/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Cerulean';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cosmo/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Cosmo';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/cyborg/bootstrap.min.css;/css/gray.css;/syntax/styles/dark.css' WHERE Name = 'Cyborg';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/flatly/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Flatly';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/journal/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Journal';
UPDATE [Theme] SET DelimitedFiles = '/css/light.css;/syntax/styles/light.css' WHERE Name = 'Light';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/litera/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Litera';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lumen/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Lumen';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/lux/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Lux';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/materia/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Materia';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/minty/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Minty';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/morph/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Morph';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/pulse/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Pulse';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/quartz/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Quartz';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sandstone/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Sandstone';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/simplex/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Simplex';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/sketchy/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Sketchy';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/slate/bootstrap.min.css;/css/gray.css;/syntax/styles/dark.css' WHERE Name = 'Slate';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/solar/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Solar';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/spacelab/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Spacelab';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/superhero/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Superhero';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/united/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'United';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/vapor/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Vapor';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/yeti/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Yeti';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/zephyr/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Zephyr';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/darkly/bootstrap.min.css;/css/dark.css;/syntax/styles/dark.css' WHERE Name = 'Darkly';
UPDATE [Theme] SET DelimitedFiles = 'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/brite/bootstrap.min.css;/css/light.css;/syntax/styles/light.css' WHERE Name = 'Technobabble';

INSERT INTO Theme(Name, DelimitedFiles, ClassNavBar, ClassNavLink, ClassDropdown, ClassBranding, EditorTheme)
SELECT
	'Technobabble',
	'https://cdnjs.cloudflare.com/ajax/libs/bootswatch/5.3.7/brite/bootstrap.min.css;/css/light.css;/syntax/styles/light.css',
	'navbar-light bg-light',
	'text-dark',
	'text-dark',
	'text-dark',
	'light'
WHERE
	NOT EXISTS (SELECT 1 FROM Theme WHERE Name = 'Technobabble');