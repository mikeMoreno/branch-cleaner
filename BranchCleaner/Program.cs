using LibGit2Sharp;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

bool verbose = false;

if (!TryGetRepository(out Repository repo))
{
    Console.WriteLine($"Failed to access git repository at the current directory.{Environment.NewLine}Confirm that the current directory is a git repository.");

    return;
}

var branchesToDelete = GetBranchesToDelete(GetLocalBranches(repo));

if (!branchesToDelete.Any())
{
    repo.Dispose();

    return;
}

Console.WriteLine($"{Environment.NewLine}Really delete the following branches?:{Environment.NewLine}");

foreach (var branch in branchesToDelete)
{
    Console.WriteLine(branch.FriendlyName);
}

Console.Write($"{Environment.NewLine}Please confirm [y/N]: ");

var response = Console.ReadLine();

if (string.IsNullOrWhiteSpace(response) || response.ToLower() != "y")
{
    Console.WriteLine("Aborting!");
    return;
}

foreach (var branch in branchesToDelete)
{
    if (verbose)
    {
        Console.WriteLine($"Deleting: {branch.FriendlyName}");
    }

    repo.Branches.Remove(branch);
}

repo.Dispose();

static bool TryGetRepository(out Repository repository)
{
    try
    {
        repository = new Repository(".");

        return true;
    }
    catch
    {
    }

    repository = null;

    return false;
}

static IEnumerable<Branch> GetLocalBranches(Repository repository)
{
    var excludedBranches = ImmutableHashSet.CreateRange(
        new List<string>()
        {
            "develop",
            "master",
        }
    );

    return repository.Branches
        .Where(b => !b.IsRemote
        && !excludedBranches.Contains(b.FriendlyName)
        && !b.FriendlyName.StartsWith("poc/")
        && !b.FriendlyName.StartsWith("release/")
        && !b.FriendlyName.StartsWith("release-candidate/")
        && !b.FriendlyName.StartsWith("local/")
        )
        .OrderBy(b => b.FriendlyName);
}

static HashSet<int> ParseInput(string input, int maxBranchIndex)
{
    input = input.Trim();

    if (Regex.IsMatch(input, "^\\d+-\\d+$"))
    {
        var indexesToDelete = new HashSet<int>();

        var firstNumber = int.Parse(input.Split('-')[0]);
        var secondNumber = int.Parse(input.Split('-')[1]);

        if (firstNumber > secondNumber)
        {
            throw new ArgumentException("Bad range given.");
        }

        if (secondNumber >= maxBranchIndex)
        {
            throw new ArgumentException("Second number is outside of max branch index.");
        }

        for (var i = firstNumber; i <= secondNumber; i++)
        {
            indexesToDelete.Add(i);
        }

        return indexesToDelete;
    }

    var branchesToDeleteParsed = input
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(i => int.Parse(i.Trim()))
        .ToHashSet();

    foreach (var index in branchesToDeleteParsed)
    {
        if (index < 1 || index >= maxBranchIndex)
        {
            throw new ArgumentException($"Bad input: {index}.");
        }
    }

    return branchesToDeleteParsed;
}

static IEnumerable<Branch> GetBranchesToDelete(IEnumerable<Branch> localBranches)
{
    var branchesToDelete = new List<Branch>();

    var branchMap = new Dictionary<int, Branch>();

    int i = 1;

    Console.WriteLine("");

    foreach (var branch in localBranches)
    {
        branchMap.Add(i, branch);

        Console.WriteLine($"[{i++}]: {branch.FriendlyName}");
    }

    Console.Write($"{Environment.NewLine}What are we deleting? (Enter integers, space-delimited, or enter an inclusive range. i.e. X-Y): ");

    var branchesToDeleteResponse = Console.ReadLine();

    HashSet<int> branchesToDeleteParsed = null;

    try
    {
        branchesToDeleteParsed = ParseInput(branchesToDeleteResponse, i);
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        Console.WriteLine("Aborting!");

        Environment.Exit(1);
    }

    foreach (var kvp in branchMap.Where(kvp => branchesToDeleteParsed.Contains(kvp.Key)).OrderBy(kvp => kvp.Key))
    {
        branchesToDelete.Add(kvp.Value);
    }

    return branchesToDelete;
}
