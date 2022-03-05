using LibGit2Sharp;

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
    return repository.Branches.Where(b => !b.IsRemote &&
                                           b.FriendlyName != "develop" &&
                                           b.FriendlyName != "master")
        .OrderBy(b => b.FriendlyName);
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

    Console.Write($"{Environment.NewLine}What are we deleting? (Enter integers, space-delimited): ");

    var branchesToDeleteResponse = Console.ReadLine();

    var branchesToDeleteParsed = branchesToDeleteResponse
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(i => int.Parse(i.Trim()))
        .ToHashSet();

    foreach (var index in branchesToDeleteParsed)
    {
        if (index < 1 || index > i)
        {
            Console.WriteLine($"Bad input: {index}");
            Console.WriteLine("Aborting!");

            Environment.Exit(1);
        }
    }

    foreach (var kvp in branchMap.Where(kvp => branchesToDeleteParsed.Contains(kvp.Key)).OrderBy(kvp => kvp.Key))
    {
        branchesToDelete.Add(kvp.Value);
    }

    return branchesToDelete;
}
