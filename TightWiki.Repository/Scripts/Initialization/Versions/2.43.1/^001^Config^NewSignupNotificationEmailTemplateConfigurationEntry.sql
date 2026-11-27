INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'),
	'Template: New Signup Notification Email',
	'<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <meta http-equiv="content-type" content="text/html; charset=utf-8" />
    <title>##SUBJECT##</title>
    <style>
        .body {
            max-width: 800px;
            min-width: 400px;
            background-color: #fbfaf8;
            word-wrap: break-word;
        }
    </style>
</head>
<body style="margin:0px; padding:0px; border: 0 none; font-size: 11px; font-family: Verdana, sans-serif; background-color: #fbfaf8;">
    <table style="width: 100%; margin: 10px auto 50px auto; border: 0px #ffffff solid; background: #fbfaf8; font-size: 14px; font-family: Verdana, sans-serif;" align="center">
        <tbody>
            <tr>
                <td style="padding: 9px; color: #000000; background: #fbfaf8;">
                    <img src="##SITEADDRESS##/File/Image/builtin::media/TightWiki_Icon_128_png" alt="##SITENAME##" />
                </td>
            </tr>
            <tr style="padding: 18px;">
                <td style="padding: 10px; vertical-align: top; background: #fbfaf8">
                    <div class="body">
                        Administrator,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp; A new account has signed up on ##SITENAME##.<br /><br />
                        <strong>Account name:</strong> ##ACCOUNTNAME##<br />
                        <strong>Email:</strong> ##ACCOUNTEMAIL##<br />
                        <strong>Name:</strong> ##PERSONNAME##<br />
                        <strong>Country:</strong> ##ACCOUNTCOUNTRY##<br />
                        <strong>Time-zone:</strong> ##ACCOUNTTIMEZONE##<br />
                        <strong>Language:</strong> ##ACCOUNTLANGUAGE##<br /><br />
                        <a href="##ACCOUNTLINK##">Click here to review the account</a>
                        <br /><br />Thanks,<br />
                        &nbsp;&nbsp;&nbsp;&nbsp;##SITENAME##
                    </div>
                </td>
            </tr>
        </tbody>
    </table>
</body>
</html>
',
	5,
	'The email sent to administrators when a new account signs up. Supports ##SUBJECT##, ##ACCOUNTNAME##, ##ACCOUNTEMAIL##, ##PERSONNAME##, ##ACCOUNTCOUNTRY##, ##ACCOUNTTIMEZONE##, ##ACCOUNTLANGUAGE##, ##ACCOUNTLINK##, ##SITENAME## and ##SITEADDRESS##.',
	0,
	1
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Template: New Signup Notification Email'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'));
