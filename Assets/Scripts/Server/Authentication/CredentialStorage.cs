using Newtonsoft.Json;
using UnityEngine;

namespace Server.Authentication
{
    public class CredentialStorage
    {
        private const string Key = "Credential";

        public void Save(
            Credential credential)
        {
            string json =
                JsonConvert.SerializeObject(
                    credential);

            PlayerPrefs.SetString(
                Key,
                json);

            PlayerPrefs.Save();
        }

        public Credential? Load()
        {
            if (!PlayerPrefs.HasKey(Key))
                return null;

            string json =
                PlayerPrefs.GetString(Key);

            return JsonConvert.DeserializeObject<Credential>(
                json);
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);

            PlayerPrefs.Save();
        }
    }
}