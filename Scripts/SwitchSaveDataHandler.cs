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

using System.IO;
using UnityEngine;
using System;
using nn.fs;
using nn.account;
using nn.hid;
using FileHandle = nn.fs.FileHandle;
using FileSystem = nn.fs.FileSystem;
using NnSaveData = nn.fs.SaveData;

public abstract class SwitchSaveDataHandler : MonoBehaviour
{
    protected const string MOUNT_NAME = "BaldiPlusSave";
    protected nn.account.Uid userId;
    protected bool isMounted = false;

    protected virtual void Start()
    {
        nn.account.Account.Initialize();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();
        
       if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }        
        nn.Result result = nn.account.Account.GetUserId(ref userId, userHandle);
        result.abortUnlessSuccess();
        result = nn.fs.SaveData.Mount(MOUNT_NAME, userId);
        if (result.IsSuccess())
        {
            isMounted = true;
            Debug.Log("Save data mounted successfully");
        }
        else
        {
            Debug.LogError($"Failed to mount save data: {result}");
            result.abortUnlessSuccess();
        }
    }

    protected virtual void OnDestroy()
    {
        if (isMounted)
        {
            FileSystem.Unmount(MOUNT_NAME);
        }
    }

    protected byte[] LoadData(string fileName)
    {
        if (!isMounted) return null;

        string filePath = $"{MOUNT_NAME}:/{fileName}";
        EntryType entryType = 0;
        nn.Result result = FileSystem.GetEntryType(ref entryType, filePath);
        
        if (FileSystem.ResultPathNotFound.Includes(result)) 
            return null;
            
        result.abortUnlessSuccess();

        FileHandle fileHandle = new FileHandle();
        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Read);
        result.abortUnlessSuccess();

        long fileSize = 0;
        result = nn.fs.File.GetSize(ref fileSize, fileHandle);
        result.abortUnlessSuccess();

        byte[] data = new byte[fileSize];
        result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        return data;
    }

    protected void SaveData(byte[] data, string fileName)
    {
        if (!isMounted) return;

        string filePath = $"{MOUNT_NAME}:/{fileName}";

#if UNITY_SWITCH
        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif

        nn.Result result = nn.fs.File.Delete(filePath);
        if (!FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        result = nn.fs.File.Create(filePath, data.Length);
        result.abortUnlessSuccess();

        FileHandle fileHandle = new FileHandle();
        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(fileHandle, 0, data, data.LongLength, WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);

        result = FileSystem.Commit(MOUNT_NAME);
        result.abortUnlessSuccess();

#if UNITY_SWITCH
        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif
    }
}