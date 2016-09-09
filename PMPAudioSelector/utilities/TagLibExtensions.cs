using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using TagLib.Id3v2;

namespace PMPAudioSelector
{
    public static class TagLibExtensions
    {
        public static void SetCustomValue(this TagLib.File tagLibFile, TagCustomKey key, object value)
        {
            string[] arrayVal = new string[] { value == null ? null : value.ToString() };

            tagLibFile.SetUserTextInformationFrameValue(key.ToString(), arrayVal);
        }

        public static string GetCustomValue(this TagLib.File tagLibFile, TagCustomKey key)
        {
            string[] arrayVal = tagLibFile.GetUserTextInformationFrameValue(key.ToString());

            if (arrayVal == null || arrayVal.Length == 0)
            {
                return null;
            }

            return arrayVal.First();
        }

        public static void SetUserTextInformationFrameValue(this TagLib.File tagLibFile, string key, string[] value)
        {
            var id3v2Tag = tagLibFile.GetOrCreateId3v2();

            // Get the frame corresponding to the key or create it if it doesn't yet exist
            var userTextInformationFrame = UserTextInformationFrame.Get(id3v2Tag, key, true);

            // Set the value
            userTextInformationFrame.Text = value;

        }

        public static string[] GetUserTextInformationFrameValue(this TagLib.File tagLibFile, string key)
        {
            var id3v2Tag = tagLibFile.GetOrCreateId3v2();

            // Get the frame corresponding to the key. Don't create it if it doesn't exist.
            var userTextInformationFrame = UserTextInformationFrame.Get(id3v2Tag, key, false);

            // Get the value
            if (userTextInformationFrame == null)
            {
                return null;
            }
            return userTextInformationFrame.Text;
        }

        /// <summary>
        /// Get the Id3v2 tag, or create if not found
        /// </summary>
        /// <param name="tagLibFile"></param>
        /// <returns></returns>
        public static TagLib.Id3v2.Tag GetOrCreateId3v2(this TagLib.File tagLibFile)
        {
            return (TagLib.Id3v2.Tag)tagLibFile.GetTag(TagTypes.Id3v2, true);
        }

    }
}
