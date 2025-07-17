using System.Text;

namespace BFormDomain.CommonCode.Utility;


public class Base32Encoder
{
    private const int InByteSize = 8;

    private const int OutByteSize = 5;

    private const string Base32Alphabet = "abcdefghijklmnopqrstuvwxyz234567";


    public static string? ToBase32String(byte[] bytes)
    {
        if (bytes == null)
        {
            return null;
        }
        else if (bytes.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(bytes.Length * InByteSize / OutByteSize);

        int bytesPosition = 0;

        int bytesSubPosition = 0;

        byte outputBase32Byte = 0;

        int outputBase32BytePosition = 0;

        while (bytesPosition < bytes.Length)
        {
            int bitsAvailableInByte = Math.Min(InByteSize - bytesSubPosition, OutByteSize - outputBase32BytePosition);
            outputBase32Byte <<= bitsAvailableInByte;
            outputBase32Byte |= (byte)(bytes[bytesPosition] >> (InByteSize - (bytesSubPosition + bitsAvailableInByte)));
            bytesSubPosition += bitsAvailableInByte;
            if (bytesSubPosition >= InByteSize)
            {
                bytesPosition++;
                bytesSubPosition = 0;
            }
            outputBase32BytePosition += bitsAvailableInByte;
            if (outputBase32BytePosition >= OutByteSize)
            {

                outputBase32Byte &= 0x1F;
                builder.Append(Base32Alphabet[outputBase32Byte]);
                outputBase32BytePosition = 0;
            }
        }

        if (outputBase32BytePosition > 0)
        {
            outputBase32Byte <<= (OutByteSize - outputBase32BytePosition);
            outputBase32Byte &= 0x1F;
            builder.Append(Base32Alphabet[outputBase32Byte]);
        }

        return builder.ToString();
    }

    public static byte[]? FromBase32String(string base32String)
    {
        if (base32String == null)
        {
            return null;
        }
        else if (base32String == string.Empty)
        {
            return Array.Empty<byte>();
        }

        string strLowerCase = base32String.ToLowerInvariant();
        byte[] outputBytes = new byte[strLowerCase.Length * OutByteSize / InByteSize];
        if (outputBytes.Length == 0)
        {
            throw new ArgumentException("Specified string is not valid Base32 format because it doesn't have enough data to construct a complete byte array");
        }

        int base32Position = 0;
        int base32SubPosition = 0;
        int outputBytePosition = 0;
        int outputByteSubPosition = 0;
        while (outputBytePosition < outputBytes.Length)
        {

            int currentBase32Byte = Base32Alphabet.IndexOf(strLowerCase[base32Position]);
            if (currentBase32Byte < 0)
            {
                throw new ArgumentException(string.Format("Specified string is not valid Base32 format because character \"{0}\" does not exist in Base32 alphabet", base32String[base32Position]));
            }


            int bitsAvailableInByte = Math.Min(OutByteSize - base32SubPosition, InByteSize - outputByteSubPosition);
            outputBytes[outputBytePosition] <<= bitsAvailableInByte;
            outputBytes[outputBytePosition] |= (byte)(currentBase32Byte >> (OutByteSize - (base32SubPosition + bitsAvailableInByte)));
            outputByteSubPosition += bitsAvailableInByte;

            if (outputByteSubPosition >= InByteSize)
            {
                outputBytePosition++;
                outputByteSubPosition = 0;
            }

            base32SubPosition += bitsAvailableInByte;
            if (base32SubPosition >= OutByteSize)
            {
                base32Position++;
                base32SubPosition = 0;
            }
        }

        return outputBytes;
    }
}