using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cleipnir.Helpers
{
    public static class FunctionalExtensions
    {

        public static T MaxFor<T>(this IEnumerable<T> ts, Func<T, int> toCompare)
            => ts.MaxFors(toCompare).Single();

        public static IEnumerable<T> MaxFors<T>(this IEnumerable<T> ts, Func<T, int> toCompare)
        {
            if (!ts.Any())
                return Enumerable.Empty<T>();

            var initial = new[] {ts.First()}.AsEnumerable();
            return ts.Skip(1).Aggregate(
                seed: initial,
                (maxsSoFar, t) => 
                    toCompare(maxsSoFar.First()) == toCompare(t) 
                        ? maxsSoFar.Append(t) 
                        : toCompare(maxsSoFar.First()) <= toCompare(t) 
                            ? new [] {t} 
                            : maxsSoFar
            );
        }

        public static T MinFor<T>(this IEnumerable<T> ts, Func<T, int> toCompare)
            => ts.MinFors(toCompare).First();

        public static T MinFor<T>(this IEnumerable<T> ts, Func<T, int> toCompare, Func<T> fallback) where T : class
            => ts.MinFors(toCompare).SingleOrDefault() ?? fallback();

        public static IEnumerable<T> MinFors<T>(this IEnumerable<T> ts, Func<T, int> toCompare)
            => ts.MaxFors(t => -toCompare(t));

        public static IEnumerable<T> DistinctFor<T, TToCompare>(this IEnumerable<T> ts, Func<T, TToCompare> selector)
        {
            var set = new HashSet<TToCompare>();

            foreach (var t in ts)
            {
                var toCompare = selector(t);
                if (set.Contains(toCompare))
                    continue;

                set.Add(toCompare);

                yield return t;
            }
        }

        public static bool Empty<T>(this IEnumerable<T> ts) => !ts.Any();

        public static bool None<T>(this IEnumerable<T> ts, Func<T, bool> predicate)
            => ts.All(t => !predicate(t));

        public static T MaxOr<T>(this IEnumerable<T> ts, T fallback) => ts.Empty() ? fallback : ts.Max();

        public static T SingleOr<T>(this IEnumerable<T> ts, T fallback)
            => ts.Empty() ? fallback : ts.First();

        public static TOut SingleOr<T, TOut>(this IEnumerable<T> ts, Func<T, TOut> selector, TOut fallback)
            => ts.Empty() ? fallback : selector(ts.First());

        public static IEnumerable<T> Append<T>(this IEnumerable<T> ts, params T[] toAppend)
            => ts.Concat(toAppend);

        public static void ForEach<T>(this IEnumerable<T> ts, Action<T> f)
        {
            foreach (var t in ts)
                f(t);
        }
        
        public static void ForEach<T>(this IEnumerable<T> ts, Action<T, int> f)
        {
            var i = 0;
            foreach (var t in ts)
                f(t, i++);
        }

        public static IEnumerable<int> RangeUntil(int to, int from = 1)
        {
            var curr = 1;
            while (curr <= to)
            {
                yield return curr;
                curr++;
            }
        } 

        public static IEnumerable<T> InvokeRepeatedly<T>(this Func<T> f, int occurrences)
            => RangeUntil(occurrences).Select(_ => f()).ToList();

        public static IEnumerable<T> InvokeAll<T>(this IEnumerable<Func<T>> toInvokes) => toInvokes.Select(f => f());
        public static void InvokeAll(this IEnumerable<Action> toInvokes) => toInvokes.ForEach(f => f());

        public static IEnumerable<int> Repeat(int occurrences) => RangeUntil(occurrences);

        public static IEnumerable<T> Repeat<T>(T toRepeat, int occurrences) => Repeat(occurrences).Select(_ => toRepeat);
        public static IEnumerable<Func<T>> Repeat<T>(Func<T> toRepeat, int occurrences) => Repeat(occurrences).Select(_ => toRepeat);

        public static Task<T> ToTask<T>(this T result)
            => Task.FromResult(result);

        public static IEnumerable<T> CastOrRemove<T>(this IEnumerable<object> ts) =>
            ts.Where(t => t is T).Cast<T>();

        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
            => Task.WhenAll(tasks);

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> elms) => elms.SelectMany(_ => _);

        public static IEnumerable<T> RemoveLast<T>(this IEnumerable<T> ts)
        {
            var queue = new Queue<T>();
            using (var enumerator = ts.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    queue.Enqueue(enumerator.Current);
                    if (queue.Count == 2)
                        yield return queue.Dequeue();
                }
            }
        }

        public static bool AllEqual<T>(this IEnumerable<T> ts) => ts.Distinct().Count() == 1;

        public static T Max<T>(this IEnumerable<T> ts, Func<T, T, T> maxSelector)
        {
            using (var enumerator = ts.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new ArgumentException("Cannot find max value of empty enumerable");

                var currMax = enumerator.Current;
                while (enumerator.MoveNext())
                    currMax = maxSelector(currMax, enumerator.Current);

                return currMax;
            }
        }

        public static string ToJson(this object o, bool indented = false) => JsonConvert.SerializeObject(o, indented ? Formatting.Indented : Formatting.None);

        public static void WriteToConsole(this string s) => Console.WriteLine(s);

        public static bool IsGarbageCollected<T>(this WeakReference<T> @ref) where T : class
        {
            return !@ref.TryGetTarget(out var _);
        }

        public static void Enqueue<T>(this Queue<T> queue, IEnumerable<T> elms) => elms.ForEach(queue.Enqueue);

        public static IEnumerable<T> WhereNotIn<T>(this IEnumerable<T> haystack, IEnumerable<T> needles)
        {
            needles = needles.ToList();
            foreach (var t in haystack)
            {
                if (!needles.Contains(t))
                    yield return t;
            }
        }

        public static IEnumerable<T> WhereNotIn<T>(this IEnumerable<T> haystack, ISet<T> needles) 
            => haystack.Where(t => !needles.Contains(t));

        public static IEnumerable<T> FilterNulls<T>(this IEnumerable<T> ts) where T : class
            => ts.Where((t => t != null));

        public static T DeserializeInto<T>(this JRaw jRaw) => JsonConvert.DeserializeObject<T>(jRaw.ToString());

        public static void SafeTry(Action a)
        {
            try
            {
                a();
            }
            catch { }
        }

        public static void Try(Action toExecute, Action<Exception> onError)
        {
            try
            {
                toExecute();
            }
            catch (Exception e)
            {
                onError(e);
            }

        }

        public static string StringJoin(this IEnumerable<string> s, string separator)
            => string.Join(separator, s.ToArray());

        public static DictionaryWithDefault<TKey, TValue> ToDictionaryWithDefault<TKey, TValue>(this IDictionary<TKey, TValue> d, Func<TKey, TValue> @default)
            => new DictionaryWithDefault<TKey, TValue>(@default, d);

        public static IEnumerable<string> FindDuplicates(this IEnumerable<string> ts)
        {
            var set = new HashSet<string>();
            foreach (var t in ts)
            {
                if (set.Contains(t))
                    yield return t;

                set.Add(t);
            }
        }

        public static int GetSequenceHashCode<T>(this IEnumerable<T> ts)
        {
            unchecked
            {
                var hash = 19;
                foreach (var t in ts)
                {
                    hash = hash * 31 + hash.GetHashCode();
                }
                return hash;
            }
        }

        public static void AddAll<T>(this ISet<T> set, IEnumerable<T> toAdd)
        {
            foreach (var t in toAdd)
                set.Add(t);
        }

        public static T CastTo<T>(this object o) => (T) o;
    }
}