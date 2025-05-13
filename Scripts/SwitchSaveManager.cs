/*      
 * MIT License
 *
 * Copyright (c) 2025 maybekoi
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the
 * Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
 * THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

// I WILL NOT PROVIDE DOCUMENTATION FOR THIS SCRIPT BECAUSE IT WAS MADE FOR PERSONAL USE. YOU'RE WELCOME TO MAKE YOUR OWN DOCUMENTATION - koi

using UnityEngine;
using System.IO;

public abstract class SwitchSaveManager : SwitchSaveDataHandler
{
    protected string GetSavePath(string fileName)
    {
#if UNITY_SWITCH
        return fileName;
#else
        return Application.persistentDataPath + "/" + fileName;
#endif
    }

    protected void SaveToFile(string fileName, string data, bool encrypt = false, string encryptionKey = "")
    {
#if UNITY_SWITCH
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
        SaveData(bytes, fileName);
#else
        if (File.Exists(GetSavePath(fileName)))
        {
            File.Delete(GetSavePath(fileName));
        }
        if (encrypt)
        {
            File.WriteAllText(GetSavePath(fileName), UnityCipher.RijndaelEncryption.Encrypt(data, encryptionKey));
        }
        else
        {
            File.WriteAllText(GetSavePath(fileName), data);
        }
#endif
    }

    protected string LoadFromFile(string fileName, bool decrypt = false, string decryptionKey = "")
    {
#if UNITY_SWITCH
        byte[] data = LoadData(fileName);
        if (data == null) return null;
        string result = System.Text.Encoding.UTF8.GetString(data);
        if (decrypt)
        {
            return UnityCipher.RijndaelEncryption.Decrypt(result, decryptionKey);
        }
        return result;
#else
        string path = GetSavePath(fileName);
        if (!File.Exists(path)) return null;
        
        string data = File.ReadAllText(path);
        if (decrypt)
        {
            return UnityCipher.RijndaelEncryption.Decrypt(data, decryptionKey);
        }
        return data;
#endif
    }

    protected bool FileExists(string fileName)
    {
#if UNITY_SWITCH
        return LoadData(fileName) != null;
#else
        return File.Exists(GetSavePath(fileName));
#endif
    }

    protected void DeleteFile(string fileName)
    {
#if UNITY_SWITCH
        SaveData(new byte[0], fileName);
#else
        string path = GetSavePath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
#endif
    }
} 