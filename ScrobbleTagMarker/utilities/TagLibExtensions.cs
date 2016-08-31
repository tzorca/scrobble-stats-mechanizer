using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagLib.Id3v2;
using System.Threading.Tasks;
using TagLib;

namespace ScrobbleStatsMechanizer
{
    internal static class TagLibExtensions
    {
        /// <summary>
        /// Returns true if the new value was different than the previous value
        /// </summary>
        /// <param name="tagLibFile"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True if the new value was different than the previous value</returns>
        public static bool SetCustomValue(this TagLib.File tagLibFile, TagCustomKey key, object value)
        {
            var previousValue = tagLibFile.GetCustomValue(key);

            string[] arrayVal = new string[] { value == null ? null : value.ToString() };

            tagLibFile.SetUserTextInformationFrameValue(key.ToString(), arrayVal);

            return previousValue != value.ToString();
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


        public static bool HasArtist(this TagLib.Tag tag)
        {
            if (tag.Performers == null)
            {
                return false;
            }

            if (tag.Performers.Length == 0)
            {
                return false;
            }

            if (tag.Performers.All(p => String.IsNullOrWhiteSpace(p)))
            {
                return false;
            }

            if (tag.Performers.All(p => p.ToLower() == "unknown artist" || p.ToLower() == "unknown"))
            {
                return false;
            }

            return true;
        }

        public static bool HasTrackTitle(this TagLib.Tag tag)
        {
            if (String.IsNullOrEmpty(tag.Title))
            {
                return false;
            }

            if (tag.Title == "Unidentified" || tag.Title == "Unspecified")
            {
                return false;
            }

            return true;
        }
    }
}
