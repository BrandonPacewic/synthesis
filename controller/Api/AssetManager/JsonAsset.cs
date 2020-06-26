﻿using Newtonsoft.Json;
using SynthesisAPI.Utilities;
using SynthesisAPI.VirtualFileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynthesisAPI.AssetManager
{
    /// <summary>
    /// Representation of a JSON asset
    /// </summary>
    public class JsonAsset : TextAsset
    {
        public JsonAsset(string name, Guid owner, Permissions perm, string sourcePath) :
            base(name, owner, perm, sourcePath) { }

        [ExposedApi]
        public TObject Deserialize<TObject>(long offset = long.MaxValue, SeekOrigin loc = SeekOrigin.Begin,
            bool retainPosition = true) // TODO
        {
            using var _ = ApiCallSource.StartExternalCall();
            return DeserializeImpl<TObject>(offset, loc, retainPosition);
        }


        internal TObject DeserializeImpl<TObject>(long offset = long.MaxValue, SeekOrigin loc = SeekOrigin.Begin,
            bool retainPosition = true) // TODO
        {
            ApiCallSource.AssertAccess(Permissions, Access.Read);
            long? returnPosition = null;
            if (offset != long.MaxValue)
            {
                if (retainPosition)
                {
                    returnPosition = SharedStream.Stream.Position;
                }
                SharedStream.Seek(offset, loc);
            }

            var obj = JsonConvert.DeserializeObject<TObject>(SharedStream.ReadToEnd());

            if (returnPosition != null)
            {
                SharedStream.Seek(returnPosition.Value);
            }
            return obj;
        }

        [ExposedApi]
        public void Serialize<TObject>(TObject obj, WriteMode writeMode = WriteMode.Overwrite)
        {
            using var _ = ApiCallSource.StartExternalCall();
            SerializeImpl<TObject>(obj, writeMode);
        }

        internal void SerializeImpl<TObject>(TObject obj, WriteMode writeMode = WriteMode.Overwrite)
        {
            ApiCallSource.AssertAccess(Permissions, Access.Write);
            if (writeMode == WriteMode.Overwrite)
            {
                SharedStream.Seek(0);
                SharedStream.SetLength(0);
            }
            else
            {
                SharedStream.Seek(0, SeekOrigin.End);
            }
            SharedStream.WriteLine(JsonConvert.SerializeObject(obj, Formatting.Indented));
        }
    }
}
