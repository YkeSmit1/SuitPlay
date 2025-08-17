using MoreLinq;

namespace Calculator.Models;

public class Cards(List<Face> data) : IEquatable<Cards>, IComparable<Cards>
{
    public readonly List<Face> Data = data;

    public override string ToString()
    {
        return string.Join("", Data.Select(Utils.CardToChar));
    }
    
    public Cards OnlySmallCardsEW()
    {
        return new Cards(Data.TakeWhile(x => Data.IndexOf(x) % 2 == 0 || x == Face.SmallCard).ToList());
    }
    
    public Cards ConvertToSmallCards(Face[] cardsNS)
    {
        var segmentsNS = cardsNS.Segment((item, prevItem, _) => (int)prevItem - (int)item > 1).ToList();
        return new Cards (Data.Select(x => !cardsNS.Contains(x) && Utils.IsSmallCard(x, segmentsNS) ? Face.SmallCard : x).ToList());
    }
    
    public Cards RemoveAfterDummy()
    {
        return new Cards(Data.IndexOf(Face.Dummy) == -1 ? Data : Data.Take(Data.IndexOf(Face.Dummy) + 1).ToList());
    }

    public Cards Clone()
    {
        return new Cards ( Data.ToList() );
    }

    public void Add(Face card) => Data.Add(card);
    public void RemoveAt(int index) => Data.RemoveAt(index);
    public IEnumerable<Face[]> Chunk(int size) => Data.Chunk(size);
    public int Count(Func<Face, bool> func) => Data.Count(func);
    public int Count() => Data.Count;
    public bool Any(Func<Face, bool> func) => Data.Any(func);
    public IEnumerable<Face> SkipLast(int count) => Data.SkipLast(count);
    public Face this[int i] => Data[i];
    public bool StartsWith(Cards play) => Data.StartsWith(play.Data);
    public Face First() => Data.First();
    public IEnumerable<Face> Take(int i) => Data.Take(i);
    public bool All(Func<Face, bool> func) => Data.All(func);
    public IEnumerable<Face> Skip(int i) => Data.Skip(i);
    public IEnumerable<(Face a, Face b)> Zip(Cards other, Func<Face, Face, (Face a, Face b)> func) => Data.Zip(other.Data, func);

    // IEquatable members

    public bool Equals(Cards other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Data.SequenceEqual(other.Data);
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Cards)obj);
    }

    public override int GetHashCode()
    {
        return Data.Aggregate(19, (current, total) => current * 31 + total.GetHashCode());
    }

    public static bool operator ==(Cards left, Cards right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Cards left, Cards right)
    {
        return !Equals(left, right);
    }
    
    // IComparable members    

    public int CompareTo(Cards other)
    {
        if (other == null)
            return -1;
        for (var i = 0; i < Data.Count && i < other.Data.Count; i++) {
            var c = Data[i].CompareTo(other[i]);
            if (c != 0) return c;
        }
        return Data.Count.CompareTo(other.Count());
    }

    public static bool operator <(Cards left, Cards right)
    {
        return Comparer<Cards>.Default.Compare(left, right) < 0;
    }

    public static bool operator >(Cards left, Cards right)
    {
        return Comparer<Cards>.Default.Compare(left, right) > 0;
    }

    public static bool operator <=(Cards left, Cards right)
    {
        return Comparer<Cards>.Default.Compare(left, right) <= 0;
    }

    public static bool operator >=(Cards left, Cards right)
    {
        return Comparer<Cards>.Default.Compare(left, right) >= 0;
    }
}