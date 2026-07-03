using System.Runtime.CompilerServices;

namespace Samples;

// SAMPLE 4 — `ref` access to a case payload: the union-struct generator's signature
// feature, preserved alongside the language union.
//
// The language feature's TryGetValue(out T) hands you a COPY of the payload. For the
// generator's zero-copy mutation use case you still need direct field access. The
// trick: expose the payload as a PUBLIC FIELD. Callers then take `ref u.Position`
// straight into the union's own storage and mutate in place — no copy, no box.
//
// Note a hard C# rule (CS8170): a struct member cannot `return ref` one of its own
// instance fields. So you cannot wrap it as `ref T AsCase => ref _field;`. The field
// itself must be public and the `ref` taken at the call site — precisely why the
// generator emits public fields.
public static class S04_RefAccess
{
    public static void Run()
    {
        Console.WriteLine("[S04] ref access to payload (in-place mutation)");

        var e = new Entity(new Player { Position = new(1, 2), Health = 100 });

        // Take a ref straight into the union's storage and mutate — zero copy.
        ref Player p = ref e.PlayerData;
        p.Position.X += 10;
        p.Health -= 25;

        Console.WriteLine($"  after ref mutate: {Describe(e)}");
    }

    static string Describe(Entity e) => e switch
    {
        Player p => $"Player pos=({p.Position.X},{p.Position.Y}) hp={p.Health}",
        Projectile pr => $"Projectile vel=({pr.Velocity.X},{pr.Velocity.Y})",
    };
}

public struct Vec2(float x, float y) { public float X = x, Y = y; }
public struct Player { public Vec2 Position; public int Health; }
public struct Projectile { public Vec2 Velocity; }

[Union]
public struct Entity : IUnion
{
    private readonly byte _tag;
    public Player PlayerData;         // public field => callers can `ref` it
    public Projectile ProjectileData;

    public Entity(Player p) { _tag = 0; ProjectileData = default; PlayerData = p; }
    public Entity(Projectile p) { _tag = 1; PlayerData = default; ProjectileData = p; }

    public object Value => _tag == 0 ? PlayerData : ProjectileData;
    public bool HasValue => true;
    public bool TryGetValue(out Player value) { value = PlayerData; return _tag == 0; }
    public bool TryGetValue(out Projectile value) { value = ProjectileData; return _tag == 1; }
}
