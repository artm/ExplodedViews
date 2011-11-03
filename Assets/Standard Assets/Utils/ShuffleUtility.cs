using System.Collections.Generic;

public class ShuffleUtility
{
    public delegate void SwapByIndex(int i, int j);
    static System.Random random = new System.Random();

    /* a fisher-yates shuffle that doesn't know what it actually shuffles */
    public static void WithSwap(int count, SwapByIndex swap)
    {
        for (int i = count; --i > 0; ) {
            int j = random.Next(i + 1); // including i
            if (i == j)
                continue;
            swap(i,j);
        }
    }

    /* simpler api for IList's */
    public static void List<T>(IList<T> list)
    {
        WithSwap(list.Count, (i,j) => { T t=list[i]; list[i]=list[j]; list[j]=t; });
    }

}

