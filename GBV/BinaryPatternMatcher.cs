using System.Numerics;
using System.Text;

namespace GBV;

public class BinaryPatternMatcher<TNum, TReturn> where TNum : IBinaryInteger<TNum>
{
    public class BpmNode
    {
        private enum AddStatus
        {
            Success,
            Abort,
            Continue
        }
        
        public readonly TNum WildcardBits;
        public readonly TNum StaticBits;
        public readonly Func<TNum, TReturn> Execute;
        private readonly List<BpmNode> _branches;

        public IReadOnlyList<BpmNode> Branches => _branches.AsReadOnly();

        private bool CanContain(BpmNode n)
        {
            return ((WildcardBits | n.WildcardBits) == WildcardBits) && ((n.StaticBits & ~WildcardBits) == StaticBits);
        }
        
        public bool Match(TNum value) => (value & ~WildcardBits) == StaticBits;

        public bool AddNode(BpmNode newNode) => AddNodeRecursive(newNode) == AddStatus.Success;

        private AddStatus AddNodeRecursive(BpmNode newNode)
        {
            if ((newNode.WildcardBits == WildcardBits) && (newNode.StaticBits == StaticBits))
                return AddStatus.Abort;

            if (!CanContain(newNode))
                return AddStatus.Continue;

            foreach (BpmNode n in _branches)
            {
                AddStatus s = n.AddNodeRecursive(newNode);
                if (s is AddStatus.Abort or AddStatus.Success)
                    return s;
            }

            foreach (BpmNode n in _branches)
                newNode.AddNodeRecursive(n);

            foreach (BpmNode n in newNode._branches)
                _branches.Remove(n);

            _branches.Add(newNode);
            return AddStatus.Success;
        }

        public BpmNode Traverse(TNum value)
        {
            foreach (BpmNode n in _branches)
                if (n.Match(value))
                    return n.Traverse(value);
            return this;
        }

        public BpmNode(TNum wildcardBits, TNum staticBits, Func<TNum, TReturn> func)
        {
            WildcardBits = wildcardBits;
            StaticBits = staticBits & ~wildcardBits;
            Execute = func;
            _branches = new List<BpmNode>();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            ToString(0, sb);

            return sb.ToString();
        }

        private void ToString(int depth, StringBuilder sb)
        {
            foreach (BpmNode n in _branches)
                n.ToString(depth + 1, sb);
            
            int bits = TypeInfo<TNum>.Size * 8;
            
            sb.Insert(0, '\n');
            
            for (int i = 0; i < depth; i++)
                sb.Insert(0, '\t');
            
            for (int i = 0; i < bits; i++)
            {
                if ((WildcardBits & (TNum.One << i)) != TNum.Zero)
                    sb.Insert(0, '*');
                else
                    sb.Insert(0, (StaticBits & (TNum.One << i)) == TNum.Zero ? '0' : '1');
            }
            
            for (int i = 0; i < depth; i++)
                sb.Insert(0, '\t');
        }
    }

    private readonly BpmNode _head;

    public BpmNode Match(TNum num) => _head.Traverse(num);

    public void AddMatch(TNum wildcardBits, TNum staticBits, Func<TNum, TReturn> call)
    {
        BpmNode n = new(wildcardBits, staticBits, call);

        _head.AddNode(n);
    }

    public void AddMatch(string bits, Func<TNum, TReturn> call)
    {
        TNum wildcard = TNum.Zero;
        TNum @static = TNum.Zero;
        foreach (char c in bits)
        {
            switch (c)
            {
                case '0':
                    wildcard <<= 1;
                    @static <<= 1;
                    break;
                case '1':
                    wildcard <<= 1;
                    @static <<= 1;
                    @static |= TNum.One;
                    break;
                case '*':
                    wildcard <<= 1;
                    @static <<= 1;
                    wildcard |= TNum.One;
                    break;
                case '_':
                    continue;
                default:
                    throw new FormatException("BinaryPattern must only contain 1, 0, *, or _");
            }
        }
    }
    
    public BinaryPatternMatcher(Func<TNum, TReturn> baseCase)
    {
        _head = new BpmNode(~TNum.Zero, TNum.Zero, baseCase);
    }

    public override string ToString() => _head.ToString();
}