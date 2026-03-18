/*
 * Create a console utility that takes a large text file, compresses it,
 * encrypts it, and saves it to disk—all without ever loading the entire file 
 * into RAM. Then, implement the reverse (Decryption -> Decompression -> Read).
 */

using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;

public class VaultService
{
    private const int BufferSize = 4096;
    public async Task ArchiveDataAsync(string inputPath, string outputPath, byte[] key, byte[] iv)
    {
        // Open the Source (Input)
        await using FileStream sourceFs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

        // Open the Destination(Output)
        await using FileStream destFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        // Layer the Decorators
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        // Chain: CryptoStream -> GZipStream -> Destination File
        // Write to the CryptoStream, which compresses via GZip, which writes to Disk.
        await using CryptoStream cryptoStream = new CryptoStream(destFs, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await using GZipStream compressionStream = new GZipStream(cryptoStream, CompressionMode.Compress);

        // Transfer buffer
        byte[] buffer = new byte[BufferSize];
        int bytesRead;
        while ((bytesRead = await sourceFs.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await compressionStream.WriteAsync(buffer, 0, bytesRead);
        }

        await compressionStream.FlushAsync();
    }

    public async Task RestoreDataAsync(string inputPath, string outputPath, byte[] key, byte[] iv)
    {
        await using var sourceFs = new FileStream(inputPath, FileMode.Open, FileAccess.Read);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        // Chain: Source File -> CryptoStream (Decrypt) -> GZipStream (Decompress)
        await using var cryptoStream = new CryptoStream(sourceFs, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await using var decompressStream = new GZipStream(cryptoStream, CompressionMode.Decompress);

        await using var destFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);

        byte[] buffer = new byte[BufferSize];
        int bytesRead;

        while ((bytesRead = await decompressStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await destFs.WriteAsync(buffer, 0, bytesRead);
        }

    }
}