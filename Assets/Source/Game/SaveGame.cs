using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;

/// <summary>
/// This component handles persistent values similar to Unity's PlayerPrefs,
/// but uses cryptography and produces binary file output.
/// </summary>
public class SaveGame
{
    /// <summary>
    /// 
    /// </summary>
    private static SaveGame _instance;

    /// <summary>
    /// 
    /// </summary>
    private Dictionary<string, string> _data;

    /// <summary>
    /// 
    /// </summary>
    private Dictionary<string, List<string>> _dataLists;

    /// <summary>
    /// Secret keys for encrypting
    /// </summary>
    private const string SECRET_CIPHER_IV   = "HR$2pIjHR$2pIj12";
    private const string SECRET_CIPHER_KEY  = "AAECAwQFBgcICQoL";

    /// <summary>
    /// 
    /// </summary>
    private const string SAVE_GAME_SUFFIX = "_savegame";

    /// <summary>
    /// 
    /// </summary>
    public static SaveGame Instance
    {
        get { return _instance ?? (_instance = new SaveGame( )); }
    }

    /// <summary>
    /// 
    /// </summary>
    private SaveGame( )
    {
        // Allocate memory storage
        _data = new Dictionary<string, string>( );
        _dataLists = new Dictionary<string, List<string>>( );
    }

    /// <summary>
    /// Encrypts and saves persistent values to file
    /// </summary>
    public void Save( )
    {
        // Notify listeners that persistent values are just about to be saved
        //NotifySave( );

        using(MemoryStream memoryStream = new MemoryStream( ))
        {
            // Dictionary to memory stream
            BinaryWriter writer = new BinaryWriter( memoryStream );

            // Write individual values
            writer.Write( _data.Count );
            foreach(KeyValuePair<string, string> entry in _data)
            {
                writer.Write( entry.Key );
                writer.Write( entry.Value );
            }

            // Write array lists
            writer.Write( _dataLists.Count );
            foreach(KeyValuePair<string, List<string>> entry in _dataLists)
            {
                writer.Write( entry.Key );
                writer.Write( entry.Value.Count );
                foreach(string subEntry in entry.Value)
                {
                    writer.Write( subEntry );
                }
            }

            // Flush and close stream
            writer.Flush( );
            writer.Close( );

            // Encrypt memory stream to file
            using(FileStream fileStream = new FileStream( Path.Combine( Application.persistentDataPath, SAVE_GAME_SUFFIX ), FileMode.Create, FileAccess.Write ))
            {
                byte[ ] cryptedBuffer = Encrypt( memoryStream.GetBuffer( ), Encoding.UTF8.GetBytes( SECRET_CIPHER_KEY ), Encoding.UTF8.GetBytes( SECRET_CIPHER_IV ) );
                fileStream.Write( cryptedBuffer, 0, cryptedBuffer.Length );
            }
        }
    }

    /// <summary>
    /// Creates a new key/value pair.
    /// </summary>
    /// <typeparam name="T">Any type that implements ToString() conversion</typeparam>
    /// <param name="key">Unique identifier</param>
    /// <param name="value">Value to be saved</param>
    /// <param name="overwrite">Overrides the existing value.</param>
    /// <returns>True if the object was saved, otherwise false..</returns>
    public bool SaveValue<T>( string key, T value, bool overwrite = true )
    {
        if(overwrite || !KeyExists( key ))
        {
            _data[ key ] = value.ToString( );

            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a new key/value pair.
    /// </summary>
    /// <typeparam name="T">Any type that implements ToString() conversion</typeparam>
    /// <param name="key">Unique identifier</param>
    /// <param name="value">Value to be saved</param>
    /// <param name="overwrite">Overrides the existing value.</param>
    /// <returns>True if the object was saved, otherwise false..</returns>
    public bool SaveList<T>( string key, IEnumerable<T> value, bool overwrite = true )
    {
        if( overwrite || !KeyExists( key ) )
        {
            List<string> strList = value.Select( t => t.ToString( ) ).ToList( );
            _dataLists[ key ] = strList;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves a value from the existing save game.
    /// </summary>
    /// <typeparam name="T">Excepted type of the result</typeparam>
    /// <param name="key">Unique key identifier.</param>
    /// <param name="outValue">The saved value.</param>
    public void GetValue<T>( string key, out T outValue )
    {
        if(!_data.ContainsKey( key ))
            throw new Exception( "[SaveGame] Failed to retrieve value. Key '" + key + "' does not exist." );

        string value = _data[ key ];
        outValue = (T)Convert.ChangeType( value, typeof( T ) );
    }



    /// <summary>
    /// Retrieves a value from the existing save game.
    /// </summary>
    /// <typeparam name="T">Excepted type of the result</typeparam>
    /// <param name="key">Unique key identifier.</param>
    /// <param name="outValue">The saved value.</param>
    public void GetValue<T>( string key, int element, out T outValue )
    {
        if( !_dataLists.ContainsKey( key ) )
            throw new Exception( "[SaveGame] Failed to retrieve value. Key '" + key + "' does not exist." );

        string value = _dataLists[ key ][element];
        outValue = (T)Convert.ChangeType( value, typeof( T ) );
    }

    /// <summary>
    /// Retrieves a list value from the existing save game.
    /// </summary>
    /// <typeparam name="T">Excepted type of the result</typeparam>
    /// <param name="key">Unique key identifier.</param>
    /// <param name="outValue">The saved list.</param>
    public void GetList<T>( string key, out List<T> outValue )
    {
        if(!_dataLists.ContainsKey( key ))
            throw new Exception( "[SaveGame] Failed to retrieve value. Key '" + key + "' does not exist." );

        outValue = new List<T>( );
        for(int i = 0; i < _dataLists[ key ].Count; i++)
        {
            outValue.Add( (T)Convert.ChangeType( _dataLists[ key ][ i ], typeof( T ) ) );
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool SaveGameExist( )
    {
        return File.Exists( Path.Combine( Application.persistentDataPath, SAVE_GAME_SUFFIX ) );
    }

    /// <summary>
    /// Loads and decrypts persistent values from file
    /// </summary>
    public void Load( )
    {
        // Clear data for safety, but it should be already empty at this point
        _data.Clear( );
        _dataLists.Clear( );

        if( !SaveGameExist( ) )
        {
            CreateNewSaveGame( );
        }
        else
        {
            Debug.Log( "[GameEngine] Loading save game..." );

            using(FileStream fileStream = new FileStream( Path.Combine( Application.persistentDataPath, SAVE_GAME_SUFFIX ), FileMode.Open, FileAccess.Read ))
            {
                // Read the source file into a byte array. 
                byte[ ] bytes = new byte[ fileStream.Length ];
                int numBytesToRead = (int)fileStream.Length;
                int numBytesRead = 0;
                while(numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead. 
                    int n = fileStream.Read( bytes, numBytesRead, numBytesToRead );

                    // Break when the end of the file is reached. 
                    if(n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }

                // Decrypt byte array
                byte[ ] decryptedBuffer = Decrypt( bytes, Encoding.UTF8.GetBytes( SECRET_CIPHER_KEY ), Encoding.UTF8.GetBytes( SECRET_CIPHER_IV ) );

                // Read binary data
                using(MemoryStream memoryStream = new MemoryStream( decryptedBuffer ))
                {
                    using(BinaryReader binaryReader = new BinaryReader( memoryStream ))
                    {
                        // Read individual values
                        int count = binaryReader.ReadInt32( );
                        for(int i = 0; i < count; i++)
                        {
                            string key = binaryReader.ReadString( );
                            string val = binaryReader.ReadString( );
                            _data.Add( key, val );
                        }

                        // Read array lists
                        int listCount = binaryReader.ReadInt32( );
                        for(int i = 0; i < listCount; i++)
                        {
                            string key = binaryReader.ReadString( );
                            int numElements = binaryReader.ReadInt32( );
                            List<string> list = new List<string>( );

                            for(int j = 0; j < numElements; j++)
                            {
                                list.Add( binaryReader.ReadString( ) );
                            }

                            _dataLists.Add( key, list );
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns true if the key provided exists.
    /// </summary>
    /// <param name="key">Unique key</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    private bool KeyExists(string key )
    {
        return _data.ContainsKey( key ) || _dataLists.ContainsKey( key );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool KeyValueExists( string key )
    {
        return _data.ContainsKey( key );
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GenerateSHA256( string value )
    {
        StringBuilder Sb = new StringBuilder();

        using(SHA256 hash = SHA256.Create( ))
        {
            Encoding enc = Encoding.UTF8;
            byte[] result = hash.ComputeHash(enc.GetBytes(value));

            foreach(byte b in result)
                Sb.Append( b.ToString( "x2" ) );
        }

        return Sb.ToString( );
    }

    /// <summary>
    /// 
    /// </summary>
    private void CreateNewSaveGame( )
    {
        Debug.Log( "[GameEngine] Creating new save game..." );

        //SaveValue(SaveGameKeys.audioMuted, false );

        //SaveValue(SaveGameKeys.teamName, "HOME");
        //SaveValue(SaveGameKeys.teamColor, 0);

        //SaveValue(SaveGameKeys.bestScore, 0);
        //SaveValue(SaveGameKeys.totalGamesPlayed, 0);

        //List<int> patternWins = new List<int>();
        //for(int i = 0; i < (int)PatternType.Count; i++)
        //    patternWins.Add(0);

        //SaveList(SaveGameKeys.pattern_wins, patternWins);

        //List<int> patternLosses = new List<int>();
        //for(int i = 0; i < (int)PatternType.Count; i++)
        //    patternLosses.Add(0);

        //SaveList(SaveGameKeys.pattern_losses, patternLosses);

        //List<int> patternServed = new List<int>();
        //for(int i = 0; i < (int)PatternType.Count; i++)
        //    patternServed.Add(0);

        //SaveList(SaveGameKeys.pattern_served, patternServed);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataToEncrypt"></param>
    /// <param name="key"></param>
    /// <param name="IV"></param>
    /// <returns></returns>
    public byte[] Encrypt(byte[] dataToEncrypt, byte[] key, byte[] IV)
    {
        // Create a MemoryStream to accept the encrypted bytes 
        MemoryStream ms = new MemoryStream();

        // Create a symmetric algorithm. 
        Rijndael alg = Rijndael.Create();

        // Now set the key and the IV. 
        alg.Key = key;
        alg.IV = IV;

        // Create a CryptoStream through which we are going to be pumping our data. 
        CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);

        // Write the data and make it do the encryption 
        cs.Write(dataToEncrypt, 0, dataToEncrypt.Length);

        // Close the crypto stream 
        cs.Close();

        // Now get the encrypted data from the MemoryStream.
        byte[] encryptedData = ms.ToArray();

        return encryptedData;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataToDecrypt"></param>
    /// <param name="key"></param>
    /// <param name="IV"></param>
    /// <returns></returns>
    public byte[] Decrypt(byte[] dataToDecrypt, byte[] key, byte[] IV)
    {
        // Create a MemoryStream that is going to accept the
        MemoryStream ms = new MemoryStream();

        // Create a symmetric algorithm. 
        Rijndael alg = Rijndael.Create();

        // Now set the key and the IV. 
        alg.Key = key;
        alg.IV = IV;

        // Create a CryptoStream through which we are going to be pumping our data. 
        CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);

        // Write the data and make it do the decryption 
        cs.Write(dataToDecrypt, 0, dataToDecrypt.Length);

        // Close the crypto stream. 
        cs.Close();

        // Now get the decrypted data from the MemoryStream.  
        byte[] decryptedData = ms.ToArray();

        return decryptedData;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cryptoKey"></param>
    /// <returns></returns>
    public string Encrypt(string text, string cryptoKey)
    {
        byte[] initVectorBytes = Encoding.UTF8.GetBytes("pemgail9uzpgzl88");
        byte[] plainTextBytes = Encoding.UTF8.GetBytes(text);
        PasswordDeriveBytes password = new PasswordDeriveBytes(cryptoKey, null);
        byte[] keyBytes = password.GetBytes(256 / 8);

        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, initVectorBytes);

        MemoryStream memoryStream = new MemoryStream();
        CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
        cryptoStream.FlushFinalBlock();
        byte[] cipherTextBytes = memoryStream.ToArray();
        memoryStream.Close();
        cryptoStream.Close();

        return Convert.ToBase64String(cipherTextBytes);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="text"></param>
    /// <param name="cryptoKey"></param>
    /// <returns></returns>
    public string Decrypt(string text, string cryptoKey)
    {
        byte[] initVectorBytes = Encoding.ASCII.GetBytes("pemgail9uzpgzl88");
        byte[] cipherTextBytes = Convert.FromBase64String(text);
        PasswordDeriveBytes password = new PasswordDeriveBytes(cryptoKey, null);
        byte[] keyBytes = password.GetBytes(256 / 8);

        RijndaelManaged symmetricKey = new RijndaelManaged();
        symmetricKey.Mode = CipherMode.CBC;
        ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initVectorBytes);

        MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
        CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
        byte[] plainTextBytes = new byte[cipherTextBytes.Length];
        int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
        memoryStream.Close();
        cryptoStream.Close();

        return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
    }
}
