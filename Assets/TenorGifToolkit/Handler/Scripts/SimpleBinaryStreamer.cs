using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

/* AHH looks like you stumbled across this :) 
 * Feel free to use this, it safe for simple classes. Not tested on anything overly complex.
 * 
 * For example, you have a class from the .Net library that unity cannot serialize.
 * And you are trying to store a persistent reference as a variable but unity wont save it between scenes etc.
 * This can help you. Just make a private byte[] of your intended class. And a public getter/setter to retrieve the class.
 * 
 * private byte[] myClassVariableData;
 * public MyClass MyClassVariable
 * {
 *      get => binaryStreamer.GetIt<MyClass>(ref myClassVariableData);
        set => binaryStreamer.SetItClass(ref myClassVariableData, ref value);
 * }
 * 
 */

namespace TenorGifToolkit.Helpers
{
    public class SimpleBinaryStreamer
    {
        public T GetIt<T>(ref byte[] data)
        {
            if (data is null) return default;

            using (MemoryStream stream = new MemoryStream(data))
            {
                if (stream.Length == 0) return default;

                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(stream);
            }
        }

        public void SetItClass<T>(ref byte[] target, ref T refernce) where T : class
        {
            if (refernce is null)
            {
                target = null;
                return;
            }
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, refernce);
                target = stream.ToArray();
            }
        }

        /// <summary>Note: cannot handle null reference</summary>
        public void SetItAny<T>(ref byte[] target, ref T refernce)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, refernce);
                target = stream.ToArray();
            }
        }
    }
}