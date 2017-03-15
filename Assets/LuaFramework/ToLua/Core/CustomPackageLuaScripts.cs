using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System;

namespace LuaInterface
{
    public class CustomPackageLuaScripts
    {
        #region 自定义简易打包;
        public class LuaIndexData
        {
            public int offset;
            public int size;
            public byte[] bytes = null;
        }

        static Dictionary<string, LuaIndexData> mLuaDict = new Dictionary<string, LuaIndexData>();
        static byte[] contentBytes = null;

        public static byte[] GetLuaBytes(ref string filePath)
        {
            GetFilePathKey(ref filePath);

            CheckLuaDictValid();

            LuaIndexData indexData = null;
            if (mLuaDict.TryGetValue(filePath, out indexData) == false)
            {
                return null;
            }

            Debugger.Log(string.Format("==== Custom GetLuaFile Success {0} ====", filePath));

            if (indexData.bytes != null) return indexData.bytes;

            CheckLuaContentValid();

            indexData.bytes = new byte[indexData.size];
            Array.Copy(contentBytes, indexData.offset, indexData.bytes, 0, indexData.size);
            return indexData.bytes;
        }

        static void GetFilePathKey(ref string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            if (filePath.EndsWith(".lua")) filePath = filePath.Substring(0, filePath.Length - 4);
            if (filePath.Contains(".")) filePath = filePath.Replace('.', '/');
            //if (filePath.Contains("\\")) filePath = filePath.Replace("\\", "/");
        }

        static void CheckLuaDictValid()
        {
            if (mLuaDict.Count > 0) return;

            TextAsset textAsset = Resources.Load<TextAsset>("index") as TextAsset;
            byte[] buffer = (textAsset != null) ? textAsset.bytes : null;

            int offset = 0;
            while (offset < buffer.Length)
            {
                LuaIndexData indexData = new LuaIndexData();
                indexData.offset = ReadInt(buffer, ref offset);
                indexData.size = ReadInt(buffer, ref offset);
                string key = ReadString(buffer, ref offset);
                if (mLuaDict.ContainsKey(key))
                {
                    Debugger.LogError("==== Reapeat Lua File is " + key + " =====!!!");
                    mLuaDict[key] = indexData;
                }
                else
                {
                    mLuaDict.Add(key, indexData);
                }
            }
        }

        static void CheckLuaContentValid()
        {
            if (contentBytes != null) return;

            TextAsset textAsset = Resources.Load<TextAsset>("content") as TextAsset;
            contentBytes = (textAsset != null) ? textAsset.bytes : null;
        }

        #region lua合并逻辑;
        public static void CombineAllLuaFiles()
        {
            string luaPath = Application.dataPath + "/Resources/";
            string indexFile = luaPath + "index.bytes";
            string conentFile = luaPath + "content.bytes";
            if (File.Exists(indexFile)) File.Delete(indexFile);
            if (File.Exists(conentFile)) File.Delete(conentFile);

            Debugger.Log("==== combine Lua path is " + luaPath + " =====!!!");

            FileStream indexFS = new FileStream(indexFile, FileMode.Create);
            FileStream contentFS = new FileStream(conentFile, FileMode.Create);


            List<string> luaFileList = new List<string>();
            string rootLuaPath = Application.dataPath + "/lua_scripts/";
            GetAllLuaFiles(rootLuaPath, ref luaFileList);

            UniformFilePathRules(ref rootLuaPath);
            CombineLuaFiles(rootLuaPath, luaFileList, indexFS, contentFS);

            indexFS.Dispose();
            indexFS.Close();
            contentFS.Dispose();
            contentFS.Close();
        }

        static void CombineLuaFiles(string rootPath, List<string> luaFileList, FileStream indexFS, FileStream contentFS)
        {
            if (indexFS == null || contentFS == null || luaFileList == null) return;

            int offset = 0;
            int size = 0;
            int combineFileCount = 0;

            for (int nIdx = 0; nIdx < luaFileList.Count; nIdx++)
            {
                string luaFilePath = luaFileList[nIdx];

                // 过滤非lua文件;
                string extension = Path.GetExtension(luaFilePath);
                if (extension != ".lua") { continue; }

                // 读取buffer;
                byte[] buffer = File.ReadAllBytes(luaFilePath);
                if (buffer == null) continue;

                string keyName = GetFilePathKey(ref rootPath, ref luaFilePath);

                Debugger.Log(string.Format("=========== Combine Lua File {0}, key is {1} ==============", luaFilePath, keyName));
  
                size = buffer.Length;
                WriteInt(indexFS, offset);
                WriteInt(indexFS, size);
                WriteString(indexFS, keyName);
                contentFS.Write(buffer, 0, buffer.Length);

                offset += size;
                combineFileCount++;
            }

            Debugger.Log(string.Format("=========== Total Combine Lua File Count is {0} ==============", combineFileCount.ToString()));
        }

        /// <summary>
        /// 统一文件路径规则;
        /// </summary>
        /// <param name="lifePath"></param>
        static void UniformFilePathRules(ref string lifePath)
        {
            if (string.IsNullOrEmpty(lifePath)) return;
            if (lifePath.Contains("\\")) lifePath = lifePath.Replace('\\', '/');
        }

        /// <summary>
        /// key格式： xxx/xxx 相对路径;
        /// </summary>
        /// <param name="rootLuaPath"></param>
        /// <param name="luaPath"></param>
        /// <returns></returns>
        static string GetFilePathKey(ref string rootLuaPath, ref string luaPath)
        {
            if(string.IsNullOrEmpty(rootLuaPath) || string.IsNullOrEmpty(luaPath))
            {
                return null;
            }

            UniformFilePathRules(ref luaPath);

            int findIndex = luaPath.IndexOf(rootLuaPath);
            if(findIndex < 0)
            {
                Debugger.LogError(string.Format("Please Check rootPath:{0} is right with subPath:{1}", rootLuaPath, luaPath));
                return null;
            }

            string keyName = luaPath.Substring(findIndex + rootLuaPath.Length);
            if (keyName.EndsWith(".lua"))
            {
                keyName = keyName.Substring(0, keyName.Length - 4);
            }
            return keyName;
        }

        static void GetAllLuaFiles(string rootLuaPath, ref List<string> pathList)
        {
            if (pathList == null || string.IsNullOrEmpty(rootLuaPath)) return;

            string[] luaFiles = Directory.GetFiles(rootLuaPath);
            if (luaFiles != null)
            {
                pathList.AddRange(luaFiles);
            }

            string[] dirs = Directory.GetDirectories(rootLuaPath);
            if (dirs == null || dirs.Length == 0) return;
            for (int nIdx = 0; nIdx < dirs.Length; nIdx++)
            {
                GetAllLuaFiles(dirs[nIdx], ref pathList);
            }
        }

        #region 二进制序列化;
        static void WriteInt(FileStream fs, int val)
        {
            if (fs == null) return;
            byte[] bytes = BitConverter.GetBytes(val);
            fs.Write(bytes, 0, bytes.Length);
        }

        static void WriteString(FileStream fs, string str)
        {
            if (fs == null) return;
            if (string.IsNullOrEmpty(str))
            {
                WriteInt(fs, 0);
                return;
            }
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            WriteInt(fs, bytes.Length);
            fs.Write(bytes, 0, bytes.Length);
        }

        static int ReadInt(byte[] bytes, ref int offset)
        {
            if (bytes == null || offset + sizeof(int) > bytes.Length) return 0;
            int nRet = BitConverter.ToInt32(bytes, offset);
            offset += sizeof(int);
            return nRet;
        }

        static string ReadString(byte[] bytes, ref int offset)
        {
            int length = ReadInt(bytes, ref offset);
            if (length == 0) return null;
            if (offset + length > bytes.Length) return null;

            string str = System.Text.Encoding.UTF8.GetString(bytes, offset, length);
            offset += length;
            return str;
        }
        #endregion

        #endregion

        #endregion
    }
}
