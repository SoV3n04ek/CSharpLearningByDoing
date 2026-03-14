using System.Text.Json;

string jsonString = File.ReadAllText("ChinookData.json");
ChinookStore store = JsonSerializer.Deserialize<ChinookStore>(
    jsonString,
    new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    })!;

var artistAlbums = store.Artists
    .Join(store.Albums,
        art => art.ArtistId,
        alb => alb.ArtistId,
        (art, alb) => new { art.Name, alb.Title });

var allTracksWhereUnitPriceIsGreaterThen099 =
    store.Tracks.Where(track => track.UnitPrice > 0.99m);

var listOfArtistsFormated = store.Artists.Select(x => $"Arist: {x.Name}");

var allAlbumsAlphabeticallyByTitle = store.Albums.OrderBy(x => x.Title);

var secondPageOf10Tracks = store.Tracks.Skip(10).Take(10);

var findFirstArtistWithAcDc = store.Artists.FirstOrDefault(x => x.Name == "AC/DC");

var findAnyWhichHaveMoreThen10Price = store.Tracks.Any(x => x.UnitPrice > 10m);

var artistAlbumsFull = store.Artists.Join(
    store.Albums, // inner list join
    artist => artist.ArtistId, // key from outer list (Artists)
    album => album.ArtistId,   // key from inner list (Albums)
    (artist, album) => new { ArtistName = artist.Name, AlbumTitle = album.Title }
);

var tracksPerAlbum = store.Tracks
    .GroupBy(t => t.AlbumId)
    .Select(g => new { AlbumId = g.Key, TrackCount = g.Count() });

// if we Imagine that Artist class had a List<Album> Albums.
// var allTracks = store.Artists.SelectMany(a => a.Album).SelectMany(alb => alb.Tracks);

var uniqueGenres = store.Tracks.Select(t => t.GenreId).Distinct();

// (GroupBy + Count) with the much faster and shorter CountBy:
// var tracksPerAlbum = store.Tracks.CountBy(t => t.AlbumId);

public record Artist(int ArtistId, string Name);
public record Album(int AlbumId, string Title, int ArtistId);
public record Track(int TrackId, string Name, int AlbumId, int GenreId, decimal UnitPrice);

public class ChinookStore
{
    public List<Artist> Artists { get; set; } = new();
    public List<Album> Albums { get; set; } = new();
    public List<Track> Tracks { get; set; } = new();
}
