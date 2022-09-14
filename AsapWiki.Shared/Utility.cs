using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace AsapWiki.Shared
{
    public static class Utility
    {
        public static string ValidationMessage(string msg) => $"&gt;&gt;&gt; {msg} &lt;&lt;&lt;";
        public static string ValidationMessage(string msg1, string msg2) => ValidationMessage(msg1) + "<br />" + ValidationMessage(msg2);
        public static string ValidationMessage(string msg1, string msg2, string msg3) => ValidationMessage(msg1) + "<br />" + ValidationMessage(msg2) + "<br />" + ValidationMessage(msg3);

        public static string GenerateRandomString()
        {
            using (var crypto = Aes.Create())
            {
                crypto.GenerateKey();
                return Convert.ToBase64String(crypto.Key).Replace("/", "").Replace("=", "").Replace("+", "");
            }
        }

        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false; // suggested by @TK-421
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input ?? "", "<.*?>", String.Empty);
        }

        public static bool AsapWikiServerPing(string host, int port, int timeout = 10000)
        {
            try
            {

                var timeToWait = TimeSpan.FromMilliseconds(timeout);

                var udpClient = new UdpClient(host, port);

                //This is the packet that the client uses to interrogate the server for its current state.
                var packet = new byte[] { 0x00, 0x01, 0x80, 0xDE, 0xEE, 0x87, 0xD4, 0x0E, 0xD2, 0x11, 0xBA, 0x96, 0x00, 0x60, 0x08, 0x90, 0x47, 0x76, 0x01 };

                int bytesSent = udpClient.Send(packet, packet.Length);

                var asyncResult = udpClient.BeginReceive(null, null);
                asyncResult.AsyncWaitHandle.WaitOne(timeToWait);
                if (asyncResult.IsCompleted)
                {
                    IPEndPoint remoteEP = null;
                    byte[] receivedData = udpClient.EndReceive(asyncResult, ref remoteEP);

                    //This is the player count, server name, level name, etc.
                    //string utfString = Encoding.ASCII.GetString(receivedData, 0, receivedData.Length);

                    return true;
                }
                else
                {
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
