using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoSoft
{
    internal class Cryptage
    {
        public Cryptage() { }
        public string Encrypt(string text, string key)
        {
            StringBuilder encrypted = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char k = key[i % key.Length];
                encrypted.Append((char)(c ^ k));
            }
            return encrypted.ToString();
        }
    }
}
