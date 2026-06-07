---
title: "`System.Random` and Infinite Monkey Theorem"
description: "Jumping along the `System.Random` output sequence with logarithmic complexity."
image: "/thumbnail.png"
---
# {{page.title}}

{{page.description}}

Published: 2026-06-01.

Originally published at [CodeProject](https://web.archive.org/web/20250820160345/https://www.codeproject.com/Articles/1239297/System-Random-and-Infinite-Monkey-Theorem), 2018-04-13.

\[[Source Code](https://github.com/sergeykorop/monkey-typewriter/)\]

## Introduction

Random number generators (RNGs) are used in a variety of software
applications but they are usually treated as a kind of &ldquo;black box&rdquo;
simply returning a series of random numbers. Sometimes, however, we
need to go deeper and get more advanced functionality such as saving
current generator state to restore it later, or predicting subsequent
random numbers given some preceding ones, etc. Solving such problems
requires more in-depth knowledge of RNG algorithms. This article
presents one useful technique which is applicable to a wide class of
RNGs and may be used to get results which are not only funny but can
also be applied in real world projects.

This article has been inspired by a joke from the
[PCG](http://www.pcg-random.org/party-tricks.html)
site: in addition to other interesting properties, that generator can
be forced to output some predefined text. Widely known
[infinite monkey theorem](https://en.wikipedia.org/wiki/Infinite_monkey_theorem)
states that such outcome from a random process is possible, but
extremely rare. Can we do something like that with less advanced
algorithm, for example, `System.Random` from .NET Framework?
Fortunately, the answer would be &ldquo;yes&rdquo;. Research paper
[Efficient Jump Ahead for $\mathbb{F}_2$-Linear Random Number Generators](http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/ARTICLES/jumpf2-printed.pdf)
by Hiroshi Haramoto, Makoto Matsumoto, Takuji Nishimura, François
Panneton, and Pierre L'Ecuyer describes a technique for navigating
sequence of random numbers from generators based on linear
recurrencies. While the main goal of their article was presentation of
the advanced algorithm involving polynomial arithmetics over second
order Galois fields, it also mentions much simpler, although less
efficient, approach using matrix multiplication. During our quest, we
will see how `System.Random` works, why its structure makes it linear,
and, finally, how that matrix multiplication is used to produce
predefined output.

## Background

It is assumed that readers have basic experience with RNGs, such as
creating them, initializing them properly to produce a reproducible
series of random numbers, getting those numbers, etc. Intermediate
level experience with C\# should be enough to understand the code. Some
familiarity with algebra (matrix multiplication) is needed when math
beyond this code is explained.

The code for the article can be built using .NET Framework 4 and
Visual Studio 2017 Community Edition. Unit tests require NUnit but you
can remove them from the project if you don't wish that framework to
be installed.

## Monkey Typewriter

Let's begin from presenting the final result: simple program that
forces standard `System.Random` to produce an English text. Archive
available with the article contains source code for a set of related
projects in one solution, _MonkeyTypewriter_. Let's open that solution
in your favorite .NET development environment and explore
_MonkeyTypewriter.cs_. This file contains code of a simple console
application. First, we can see two helper functions: `GetSeries()` and
`DumpSeries()`. Implementation details of these functions are not
important for our goal so we just briefly describe their purpose.

```csharp
IEnumerable<byte> GetSeries(Random rng, uint size)
```

`GetSeries()` is an adapter function which converts a series of random
32-bit integers produced by an instance of `System.Random` into the
byte sequence of the specified size.

```csharp
void DumpSeries(IEnumerator<byte> seriesIter, uint pos, uint size)
```

`DumpSeries()` takes an iterator to byte stream and outputs its
contents to console in a hexadecimal and ASCII formats replacing
non-printable ASCII codes with dots.

And now, here is the `Main()` method which deserves more detailed
description. First, we create an instance of standard .NET random
generator and set up its internal state with an array of magic numbers
`initialState`. We can see the use of method `SetState()` which is not
a part of the `System.Random`. This method has been implemented as an
extension to `System.Random` and can be found in class `RandomExt`. It
will be explained later.

```csharp
Random rng = new Random();
			
int [] initialState = {...};

rng.SetState(initialState);
```

After that, we are going to produce a sequence of random bytes and
print it. To make output more concise, let's print the head of the
sequence, then skip some bytes and, finally, output a piece of data
where we expect to see something interesting. This process is managed
by a set of counters: the header should be `startingChunkSize` bytes
long followed by `skippedChunkSize` skipped bytes, and, finally, we
resume output `payloadPaddingSize` bytes before `payloadOffset` where
we are going to inspect `payloadSize` bytes.

```csharp
const uint startingChunkSize  = 100;
const uint payloadOffset      = 1000;
const uint payloadSize        = 250;
const uint payloadPaddingSize = 50;
const uint skippedChunkSize   = payloadOffset - payloadPaddingSize - startingChunkSize;
			
var series = GetSeries(rng, payloadOffset + payloadSize);
			
var seriesIter = series.GetEnumerator();
			
DumpSeries(seriesIter, 0, startingChunkSize);
			
Console.WriteLine("\nSkipping {0} bytes...\n", skippedChunkSize);

for(int i = 0; i < skippedChunkSize; i++)
    seriesIter.MoveNext();
			
DumpSeries(seriesIter, startingChunkSize + skippedChunkSize, payloadPaddingSize + payloadSize);
```

The output of this program should look like this:

```text
00000000 79 37 CE 3E 48 DE A6 02 C4 84 40 06 DC 78 58 36 y7.>H.....@..xX6
00000010 26 F8 DC 68 5F 05 1C 74 EA CB 5D 53 11 90 62 6B &..h_..t..]S..bk
00000020 D6 11 9B 5E 27 29 30 54 43 62 D5 6B 1A 1C E2 1C ...^')0TCb.k....
00000030 16 7C D1 7B D3 9A 1E 50 66 E0 F2 7B 9D 5E 0C 1B .|.{...Pf..{.^..
00000040 CE 96 7A 2F 40 E4 E5 43 6E EB 3C 74 60 7C 53 79 ..z/@..Cn.<t`|Sy
00000050 89 74 24 6F 17 19 D5 31 90 A2 E7 41 54 0B 43 22 .t$o...1...AT.C"
00000060 16 F2 4B 4F                                     ..KO            

Skipping 850 bytes...

000003B6                   81 66 DF 9E 85 1F CA D2 DF 4E       .f.......N
000003C0 E3 85 CF 4E DB 8B A3 33 6F E1 A7 52 6E 4D D4 46 ...N...3o..RnM.F
000003D0 1B EA FA 66 C1 55 39 51 E1 99 19 16 DA 05 73 7E ...f.U9Q......s~
000003E0 7C 72 6E 44 C7 35 C0 7B 4D 6F 6E 6B 65 79 20 68 |rnD.5.{Monkey h
000003F0 69 74 74 69 6E 67 20 6B 65 79 73 20 61 74 20 72 itting keys at r
00000400 61 6E 64 6F 6D 20 6F 6E 20 61 20 74 79 70 65 77 andom on a typew
00000410 72 69 74 65 72 20 6B 65 79 62 6F 61 72 64 20 66 riter keyboard f
00000420 6F 72 20 61 6E 20 69 6E 66 69 6E 69 74 65 20 61 or an infinite a
00000430 6D 6F 75 6E 74 20 6F 66 20 74 69 6D 65 20 77 69 mount of time wi
00000440 6C 6C 20 61 6C 6D 6F 73 74 20 73 75 72 65 6C 79 ll almost surely
00000450 20 74 79 70 65 20 61 20 67 69 76 65 6E 20 74 65  type a given te
00000460 78 74 2C 20 73 75 63 68 20 61 73 20 74 68 65 20 xt, such as the 
00000470 63 6F 6D 70 6C 65 74 65 20 77 6F 72 6B 73 20 6F complete works o
00000480 66 20 57 69 6C 6C 69 61 6D 20 53 68 61 6B 65 73 f William Shakes
00000490 70 65 61 72 65 2E 20 28 57 69 6B 69 70 65 64 69 peare. (Wikipedi
000004A0 61 29 34 52 FC 7C 87 6C FE DA B3 65 AC C9 91 78 a)4R.|.l...e...x
000004B0 5A EC CA 67 6C 34 F9 34 6D 96 FD 0F 07 52 FF 5D Z..gl4.4m....R.]
000004C0 A7 C1 56 0E E8 4E F7 01 F9 0C 00 07 FC 06 05 76 ..V..N.........v
000004D0 F9 46 AD 75 F2 13 07 27 41 00 A7 01 FC 4D 03 4F .F.u...'A....M.O
000004E0 06 B7                                           ..              
```

Unbelievable! Starting from the offset `0x3E8` (that is 1000), we see
an excerpt from
[Wikipedia article](https://en.wikipedia.org/wiki/Infinite_monkey_theorem)
 about Infinite monkey theorem which has been produced by
unmodified library implementation of well-known RNG algorithm. The
secret sauce is definitely that tricky initialization sequence which
will be cooked in detail in the next sections.

## Exploring RNG Internals

For beginning, let's figure out which algorithm is used in
`System.Random`. Where can we get its description? There is an
[open source implementation](https://github.com/dotnet/coreclr)
of the .NET Framework available from GitHub. Code for `System.Random`
can be found in _Random.cs_. To be precise, we will use
[this  revision](https://github.com/dotnet/coreclr/blob/6c4172449dd5d1ab55c543dd37843d4decb5aa3f/src/mscorlib/shared/System/Random.cs)
in our experiments.

Looking at the `InternalSample()` method, we can identify the
algorithm: it is so called subtractive
[lagged Fibonacci generator](https://en.wikipedia.org/wiki/Lagged_Fibonacci_generator). 
This generator produces the next random number with the following formula:

$$
\label{eq:lf} x_i = x_{i-55} - x_{i-34} \pmod{2^{31}-1}. \tag{1}
$$

Note that lag values, `55` and `34`, are specific to this
implementation but general ideas discussed in this article are
applicable to the entire lagged Fibonacci family of RNGs. Putting that
formula into the code, we have to save the last 55 generated random
numbers and keep updating them every time we produce the next one.
[Circular buffer](https://en.wikipedia.org/wiki/Circular_buffer)
would be a perfect match for this task. As we can see in
the code, buffer of size `56` is allocated as `_seedArray` and last
used item is pointed by `_inext` which is incremented every time we
add new item to the buffer. Auxiliary index `_inextp` denotes the
position of the second lag and follows `_inext` keeping the distance.

Our first improvement to library code would be methods to keep and
restore the internal state of the generator. Doing it in the evident
way, we should keep all `private` data as is, an array and two
indices. There is a different approach, however, which would be more
convenient later: saving the state of the queue in linear order, from
its beginning element to the last to fill the destination buffer
without wrapping around its boundary.

For convenience, our code working with RNG state will be implemented
as extension methods in class `RandomExt`. Since RNG data fields are
`private`, we will have to access them using reflection. For each data
field, we should obtain `FieldInfo`. These helpers can be made
`static` so we can initialize them in `static` constructor of the
`RandomExt`.

```csharp
private static readonly System.Reflection.FieldInfo seedArrayField;
private static readonly System.Reflection.FieldInfo inextField;
private static readonly System.Reflection.FieldInfo inextpField;

static RandomExt()
{
    Type rngType = typeof(Random);

    seedArrayField = rngType.GetField("SeedArray", System.Reflection.BindingFlags.NonPublic | 
                     System.Reflection.BindingFlags.Instance);
    inextField     = rngType.GetField("inext", System.Reflection.BindingFlags.NonPublic | 
                     System.Reflection.BindingFlags.Instance);
    inextpField    = rngType.GetField("inextp", System.Reflection.BindingFlags.NonPublic | 
                     System.Reflection.BindingFlags.Instance);
}
```

Note that field names in the code above are different from those in
code from GitHub which we used for reference. It appeared that .NET
Framework 4 installed from Windows SDK differs from open source
implementation and uses field names without underscores. Fortunately,
that was the only difference discovered so code based on open source
framework implementation worked well with the official one. Method
`GetState()` retrieving state from given instance of `System.Random`
is implemented as follows:

```csharp
public static int[] GetState(this Random rng)
{
    int   inext      = (int)inextField.GetValue(rng);
    int[] seedArray  = (int[])seedArrayField.GetValue(rng);
			
    int[] state = new int[seedArray.Length - 1];
			
    int upperChunkSize = seedArray.Length - (inext + 1);

    Array.Copy(seedArray, inext + 1, state, 0, upperChunkSize);
    Array.Copy(seedArray, 1, state, upperChunkSize, inext);
			
    return state;
}
```

We save state as raw integer array to simplify further
manipulations. For use in production code, it would be better to keep
that state within some opaque object. Restoring RNG state from saved
data is even simpler.

```csharp
public static void SetState(this Random rng, int[] state)
{
    int[] seedArray = (int[])seedArrayField.GetValue(rng);
			
    state.CopyTo(seedArray, 1);
			
    inextField.SetValue(rng, 0);
    inextpField.SetValue(rng, 21);
}
```

Let's come back to our experiments and see how internal state of the
RNG is changed when producing random numbers. Demo program _Explore.cs_
uses extension method `GetState()`
to produce 4 random numbers saving RNG state before getting each of
them.

```text
5584833E
12094017
1010FBBA
42E9F096
```

After that, those states are printed aligned to each other with
`CompareStates()`. Parts of the output are not represented in this
article for brevity.

```text
 0, 12501C5F, 0261CFD7, 0EE1DDBA, 305F5F61, 6B945933
 1, 0261CFD7, 0EE1DDBA, 305F5F61, 6B945933, 5EFEC028
 2, 0EE1DDBA, 305F5F61, 6B945933, 5EFEC028, 0882DB09
 3, 305F5F61, 6B945933, 5EFEC028, 0882DB09, 58BFEC38
 4, 6B945933, 5EFEC028, 0882DB09, 58BFEC38, 7186A68A
 5, 5EFEC028, 0882DB09, 58BFEC38, 7186A68A, 119335E0
 6, 0882DB09, 58BFEC38, 7186A68A, 119335E0, 5AB9802F
 7, 58BFEC38, 7186A68A, 119335E0, 5AB9802F, 085EDD91
 8, 7186A68A, 119335E0, 5AB9802F, 085EDD91, 464A53F4
 9, 119335E0, 5AB9802F, 085EDD91, 464A53F4, 25F50014
10, 5AB9802F, 085EDD91, 464A53F4, 25F50014, 0D71DBE2
...
20, 25C17A83, 3CCB9920, 70588FBF, 7ED0E1FF, 6D756ECA
21, 3CCB9920, 70588FBF, 7ED0E1FF, 6D756ECA, 56051885
22, 70588FBF, 7ED0E1FF, 6D756ECA, 56051885, 3D621D24
23, 7ED0E1FF, 6D756ECA, 56051885, 3D621D24, 2BC9713B
24, 6D756ECA, 56051885, 3D621D24, 2BC9713B, 171877FD
25, 56051885, 3D621D24, 2BC9713B, 171877FD, 5B4C72F1
...
45, 04FCC85C, 10E756FA, 37BB801B, 50C25426, 042EA0A6
46, 10E756FA, 37BB801B, 50C25426, 042EA0A6, 674DAB56
47, 37BB801B, 50C25426, 042EA0A6, 674DAB56, 577BC5A8
48, 50C25426, 042EA0A6, 674DAB56, 577BC5A8, 7A98A95A
49, 042EA0A6, 674DAB56, 577BC5A8, 7A98A95A, 4F93B56B
50, 674DAB56, 577BC5A8, 7A98A95A, 4F93B56B, 0042832E
51, 577BC5A8, 7A98A95A, 4F93B56B, 0042832E, 5584833E
52, 7A98A95A, 4F93B56B, 0042832E, 5584833E, 12094017
53, 4F93B56B, 0042832E, 5584833E, 12094017, 1010FBBA
54, 0042832E, 5584833E, 12094017, 1010FBBA, 42E9F096
```

First random number, `0x5584833E`, is calculated taking the
0<sup>th</sup> item from the buffer, `0x12501C5F`, and subtracting the
21<sup>st</sup> item, `0x3CCB9920`, modulo $2^{31}-1$:

$$
\mathrm{12501C5F}_{16} - \mathrm{3CCB9920}_{16} = \mathrm{D584833F}_{16} \equiv \mathrm{5584833E}_{16} \pmod{2^{31}-1}
$$

If we compare RNG state before producing this number (1<sup>st</sup>
column) with the state after that (2<sup>nd</sup> column), we can see
that the 0<sup>th</sup> item from the initial state has been dropped,
all subsequent numbers were shifted up one position and the last item
in the queue is filled with recently produced random number. Doing it
several times, we can see the same pattern. Now we are ready to use
this information for making an alternative mathematical model of this
RNG which will have some important benefits.

## Vector Representation and Linear RNGs

Our initial formula for lagged Fibonacci generator $(\ref{eq:lf})$
defined it via a sequence of integers $\{x_i\}$ where we have to keep
some history to calculate next $x_i$. Doing that in a clever way with
a circular buffer is merely an implementation detail. Someone could
imagine recursive algorithm recalculating sequence from the beginning
twice every time we need the next random number. While being horribly
inefficient, this way is also legitimate.

Going further, we can treat our circular buffer as a whole entity
varying in time. That is, let's consider it being a numeric vector of
length 55 (we will use zero-based item indices here which is slightly
unusual for mathematicians but matches the code better):

$$
X = \langle X_0, X_1, \ldots X_{54}\rangle, \quad 0 \le X_i< 2^{31}-1.
$$

In this case, we can define a transition function $s$ making next
state from preceding:

$$
X^{(i)} = s(X^{({i-1})}).
$$

We can also define a function $r$ mapping random state $X^{(i)}$ to
the $i$<sup>th</sup> random number $x_i$:

$$
x_i = r(X^{(i)}).
$$

In case of lagged Fibonacci, the latter is straightforward and simply
gets the 55<sup>th</sup> component of the $i$<sup>th</sup> state:

$$
x_i = X^{(i)}_{54}.
$$

Now we should define $X' = s(X)$:

$$
X'_i = \begin{cases}
  X_{i+1},                      & 0 \le i < 53, \\
  X_0 - X_{21} \pmod{2^{31}-1}, & i = 54.
\end{cases}
$$

This definition can be rewritten as follows: let's treat each state
component as a sum of all past state components multiplied by some
constants. This way, we can represent both cases above uniformly using
simple arithmetics without any conditions. Indeed, in both cases, we
can mask unneeded components by multiplying them by zeros while needed
components take their coefficients naturally from the definition
above:

$$
\begin{align*}
     X'_0 &= 0\cdot X_0 + 1\cdot X_1 + 0\cdot X_2 + 0\cdot X_3 + \cdots + 0\cdot X_{20} + 0\cdot X_{21} + 0\cdot X_{22} + \cdots + 0\cdot X_{54}\pmod{2^{31}-1},\\
     X'_1 &= 0\cdot X_0 + 0\cdot X_1 + 1\cdot X_2 + 0\cdot X_3 + \cdots + 0\cdot X_{20} + 0\cdot X_{21} + 0\cdot X_{22} + \cdots + 0\cdot X_{54}\pmod{2^{31}-1},\\
          &\vdots \\
  X'_{53} &= 0\cdot X_0 + 0\cdot X_1 + 0\cdot X_2 + 0\cdot X_3 + \cdots + 0\cdot X_{20} + 0\cdot X_{21} + 0\cdot X_{22} + \cdots + 1\cdot X_{54}\pmod{2^{31}-1},\\
  X'_{54} &= 1\cdot X_0 + 0\cdot X_1 + 0\cdot X_2 + 0\cdot X_3 + \cdots + 0\cdot X_{20} - 1\cdot X_{21} + 0\cdot X_{22} + \cdots + 0\cdot X_{54}\pmod{2^{31}-1},\\
\end{align*}
$$

This, in turn, is a [matrix product](https://en.wikipedia.org/wiki/Matrix_multiplication)
of $X$ with constant matrix $A$:

$$
\label{eq:linrng} X' = AX, \tag{2}
$$

where $A$ specific for our lagged Fibonacci RNG is:

$$
\label{eq:stepforward}
a_{ij} = \begin{cases}
  1, & 0 \le i \le 53, j = i + 1,\\
  1, & i = 54, j = 0,\\
 -1, & i = 54, j = 21,\\ 0, & \mbox{otherwise}.
\end{cases} \tag{3}
$$

Different $A\mbox{s}$ denote different RNG algorithms which may be
good or bad. For example:

$$
\label{eq:stepbackward}
a_{ij} = \begin{cases}
  1, & i = 0, j = 54,\\
  1, & i = 0, j = 20,\\
  1, & 1 \le i \le 54, j = i - 1,\\
  0, & \mbox{otherwise}
\end{cases} \tag{4}
$$

produces the same sequence of random numbers as `System.Random` but in
reverse direction. Similarly, $A=I$
([identity matrix](https://en.wikipedia.org/wiki/Identity_matrix))
defines degraded case when RNG produces sequence of constant numbers.

Family of RNGs whose algorithm can be represented as $(\ref{eq:linrng})$ is known as
_linear random number generators_. 
Besides lagged Fibonacci, this family also includes such popular algorithms as
[Mersenne Twister](https://en.wikipedia.org/wiki/Mersenne_Twister)
or [xorshift](https://en.wikipedia.org/wiki/Xorshift).

Now it's time to ask a natural question: aren't those matrix
multiplications a waste of computational resources? Yes, indeed,
lagged Fibonacci RNG implemented using circular buffer can be stepped
forward (or backward) with single subtraction/addition of buffer items
plus some amount of work needed to move a pair of buffer
pointers. Matrix multiplication, at the same time, would require in
general case
[at least](https://en.wikipedia.org/wiki/Matrix_multiplication#Complexity)
$O(k^2)$ operations for matrix of size $k$. Given that
matrix $A$ is sparse, we can get better performance if we eliminate
unneeded operations but this will eventually lead us to the original
algorithm.

Matrix representation, however, has its strong side mentioned in the 
[research paper](http://www.math.sci.hiroshima-u.ac.jp/~m-mat/MT/ARTICLES/jumpf2-printed.pdf)
introduced at the beginning of the article. Let's start from some
initial state $X^{(0)}$ and do $n$ steps:

$$
\begin{align*}
   X^{(1)} &= AX^{(0)},\\
   X^{(2)} &= AX^{(1)} &= AAX^{(0)} &= A^2X^{(0)},\\
           &\vdots\\
   X^{(n)} &= AX^{(n-1)} &= A\ldots AX^{(0)} &= A^n X^{(0)}.\\
\end{align*}
$$

It means that we can obtain the $n$<sup>th</sup> random number right
from initial state if we know the corresponding $A^n$. And there is
[well-known algorithm](https://en.wikipedia.org/wiki/Exponentiation_by_squaring)
which can compute that power in $O(\log n)$ steps. That is, we can
jump $n$ steps forward or backward along our random sequence with
logarithmic complexity. At the same time, doing the same by taking
those $n$ steps directly has linear complexity. Given $n$ is large,
logarithmic approach will outperform linear even with large constant
factor imposed by matrix multiplication under the hood.

`RandomExt` contains some helper functions based on theory above. For
example, `ForwardStep()` and `BackwardStep()` produce matrices
$(\ref{eq:stepforward})$ and $(\ref{eq:stepbackward})$,
respectively. There are other functions implementing matrix
multiplication and exponentiation as well. They can be combined to
produce the needed transition matrix which is later applied to random
state saved by `GetState()` and then loaded back to RNG with
`SetState()`. This approach looks a bit bare-boned but it seems be
enough for a proof of concept.

## From Academia to Production

Let's use theoretical backgrounds of linear RNGs to develop some
useful code. For a beginning, let's explain in detail how that trick
from section [Monkey Typewriter](#monkey-typewriter) was
implemented. As we remember, state of `System.Random` consists of 55
32-bit integers so we are able to put 220 bytes of data there. Not all
possible combinatons of bytes are allowed, however: each integer
should lie within $[0, 2^{31}-1)$ range. Also, state vector can't be
zero since it ruines the generator (formula $(\ref{eq:lf})$ starts
producing all zeros in this case). If we use only byte values from $0$
to $127$, the first restriction will be satisfied, and this range
would be enough to represent message encoded in old good 7-bit
ASCII. Regarding the second restriction, we should just keep it in
mind.

After filling state vector with needed payload, we can use matrix
multiplication as described above to step back for 55 steps and get a
new state. If we set this new state to random generator and start
retrieving random numbers from that RNG, after 55 steps we will end up
with initial state while all its components will be output as random
numbers from the generator. That's all we need to force RNG to produce
some predefined sequence! In order to make this trick more impressing,
we can also step back more steps to make our predefined content appear
after some amount of random data.

Code in _Prepare.cs_ implements this _modus operandi_ exactly. This
console application takes the needed phrase and its offset in future
random stream as command line parameters. To keep things simple, it is
assumed that offset is a multiple of 4 so payload starts at the state
item's boundary. Then helper methods from `RandomExt` are used to
calculate transition matrix for the needed number of steps and,
finally, initial state vector which should be set to RNG to produce
the specified phrase at the specified offset. This vector is then
printed to the standard output and can be inserted by hands to the
source code of _MonkeyTypewriter_ we explored first.

We can also use this theory to implement multiple _streams_ of random
numbers. That is, to produce a number of random sequences from one
seed value. How could it be implemented? Let's have some RNG with
period $N$ and choose some number of streams $s$ ($s \ll N$) we are
going to use. We can divide the original sequence to $s$ groups of
consequtive numbers, each with $\lfloor \frac{N}{s} \rfloor$ items,
using each group to produce a stream.

Implementing this idea is straightforward as long as you have an
ability to move to a given position in random sequence, and we have
just learned how to do that.

Using streams is convenient when you need to orchestrate multiple RNGs
working togehter, for example, in distributed Monte-Carlo simulations,
etc. Our goal is making sure that multiple RNGs won't produce the same
numbers.

One possible approach would be using different random seeds to
initialize those RNGs. We will have to implement some seed management
scheme, for example, designate some &ldquo;master worker&rdquo; which is
responsible for seeding others.

We could also use local RNG on each worker to generate random seed for
distributed engine. It is possible to get seed collision in this case
but it's probability is quite low. More important, this approach lacks
reproducibility: changing number of workers or even running your
computations one more time, you will get a different set of random
numbers.

Any seed-based scenario also has one more problem: we can't be sure
that sequences of random numbers produced from different seeds won't
overlap too early. The probability of such overlap is quite low,
however.

Anyway, this initialization code adds its share to overall
complexity. Using independent streams, from the other hand, looks much
simpler. We will use only one random seed common for all workers. Each
worker still be responsible for getting proper stream index but unlike
random seeds, stream indices can be calculated by workers with more
deterministic algorithm (e.g., using MPI rank). Also, with streams, we
know for sure when they start overlapping so we can decide whether it
is safe for us or not.

These two approaches can be combined giving us even more
&ldquo;degrees of freedom&rdquo;.

## Epilogue

At the end of our journey, let's summarize what we have learned. We
have seen how open source helps in understanding our tools, how to
tailor closed-source library to our needs using such C\# and .NET
features as extension methods and reflection. We have also touched
such useful data structure as
[circular buffer](https://en.wikipedia.org/wiki/Circular_buffer)
and such elegant algorithm as
[exponentiation by squaring](https://en.wikipedia.org/wiki/Exponentiation_by_squaring).
I have been really surprized by
[progress in matrix multiplication](https://en.wikipedia.org/wiki/Matrix_multiplication#Complexity)
since Strassen's algorithm. Also, now we have more detailed knowledge
of algorithm beyond `System.Random` which may prevent us from using it
in a wrong way. We have seen the common properties this algorithm
shares with some others (linearity) and how those properties can be
used to navigate along random sequence in both directions. And
finally, we applied this theory to make RNG producing funny output and
also sketched more useful idea of independent random streams and their
use in distributed computing.

## See Also

For more information about jumping along the linear random streams,
including more efficient algorithm based on polynomial arithmetic, see

 * [XorShift Jump 101, Part 1: Matrix Multiplication](https://web.archive.org/web/20250820160345/https://www.codeproject.com/Articles/5264513/XorShift-Jump-101-Part-1-Matrix-Multiplication)

 * [XorShift Jump 101, Part 2: Polynomial Arithmetic](https://web.archive.org/web/20250820160345/https://www.codeproject.com/Articles/5265915/XorShift-Jump-101-Part-2-Polynomial-Arithmetic)

## History

* 13<sup>th</sup> April, 2018: Initial version

* 04<sup>th</sup> May, 2020: Added "See Also" section

