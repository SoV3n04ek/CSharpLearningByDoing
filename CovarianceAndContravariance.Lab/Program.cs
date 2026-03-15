public class Member
{
    public string Name { get; set; }
}

public class PremiumMember : Member
{
    public DateTime Expiry { get; set; }
}


// Covariant Result Interface
public interface IResult<out T>
{
    T Value { get; }
    bool Success { get; }
}

public class Result<T> : IResult<T>
{
    public T Value { get; set; }
    public bool Success { get; set; }
}

public class EnhancedMemberService
{
    // if we would use List<T> return type it makes this invariant
    // in sutation with List<T> if we would return premiums
    // we will get an ERROR:
    // Cannot implicitly convert List<PremiumMember> to List<Member>
    // we would be forced to do this (wasteful memory/CPU):
    // return premiums.Cast<Member>().ToList();

    // Flexible way: IEnumerable is covariant.
    // IResult<T> is also covariant.
    public IResult<IEnumerable<Member>> GetMember()
    {
        var premiums = new List<PremiumMember> { new PremiumMember { Name = "Bob" } };

        // This works perfectly because of 'out' keywords
        return new Result<List<PremiumMember>>
        {
            Value = premiums,
            Success = true
        };
    }
}