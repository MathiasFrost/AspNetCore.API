using AspNetCore.API.Database;
using AspNetCore.API.Models;
using MediatR;

namespace AspNetCore.API.Handlers;

public sealed class GenerateWorldsRequest : INotification
{
    internal int WorldsToGenerate { get; init; }
}

public sealed class GenerateWorldsHandler : INotificationHandler<GenerateWorldsRequest>
{
    private readonly AspNetCoreDb _aspNetCoreDb;

    public GenerateWorldsHandler(AspNetCoreDb aspNetCoreDb) => _aspNetCoreDb = aspNetCoreDb;

    public async Task Handle(GenerateWorldsRequest notification, CancellationToken cancellationToken)
    {
        IEnumerable<World> newWorlds = Enumerable.Range(0, notification.WorldsToGenerate)
            .Select(static i =>
            {
                var rand = new Random(i);

                string name = Names[rand.Next(Names.Length)];
                decimal avgSurfaceTemp = new(Math.Round(rand.NextDouble() * 300.00 - 200.00, 2));

                byte retries = 0;
                long population;
                do population = rand.NextInt64(1, 10_000_000_000);
                while (population > 100_000_000 && retries++ < 100);

                string ecosystem = Ecosystems[rand.Next(Ecosystems.Length)];
                string theme = Themes[rand.Next(Themes.Length)];

                return new World { Name = name, AvgSurfaceTemp = avgSurfaceTemp, Population = population, Ecosystem = ecosystem, Theme = theme };
            });

        await _aspNetCoreDb
            .Sql("""
                 INSERT INTO main.World (Name, AvgSurfaceTemp, Population, Ecosystem, Theme)
                 VALUES (@Name, @AvgSurfaceTemp, @Population, @Ecosystem, @Theme)
                 """)
            .WithParams(newWorlds)
            .Execute(cancellationToken);
    }

    // ReSharper disable StringLiteralTypo
    private static readonly string[] Names = {
        "Xerovia",
        "Quorilis",
        "Zenthara",
        "Vortexis",
        "Astora",
        "Bluethor",
        "Elysara",
        "Cryomar",
        "Orionis",
        "Lunovis",
        "Solara",
        "Nebulix",
        "Vespera",
        "Corusca",
        "Plutara",
        "Thulea",
        "Serenus",
        "Omicrona",
        "Xanadu",
        "Calypsos"
    };

    private static readonly string[] Themes = {
        "Cyberpunk", //	Futuristic cities marked by high-tech and low-life, often featuring anti-corporate narratives.
        "Corporate Wild West", // A fusion of corporate influence and frontier life, where businesses operate with little regulation.
        "Western", // Set in the American frontier, often featuring cowboys, outlaws, and desert landscapes.
        "Viking", // Inspired by Norse mythology and history, featuring warriors, seafaring, and gods.
        "Sports", // Centered around competitive games or athletic endeavors, often with an underdog story.
        "Pirate", // Seafaring adventures featuring pirates, treasure hunting, and naval combat.
        "High Fantasy", // Medieval-like worlds with magic, dragons, and epic quests.
        "Gothic", // Dark and mysterious settings, often featuring elements of horror or the supernatural.
        "Steampunk", //	An alternate history featuring steam-powered machinery and Victorian aesthetics.
        "Post-Apocalyptic", // Following the collapse of civilization, often featuring survivors in a wasteland.
        "Alien Invasion", // Extraterrestrial beings attempt to invade or colonize Earth or another planet.
        "Noir", // Crime dramas often set in urban environments, featuring morally ambiguous characters.
        "Space Opera", // Epic adventures set in outer space, often featuring battles, advanced technology, and empires.
        "Utopian/Dystopian", // Idealized or flawed societies, often exploring social and political structures.
        "Martial Arts", // Focused on hand-to-hand combat, spiritual disciplines, and often Eastern philosophies.
        "Time Travel", // Involves characters moving between different times, often to change or preserve events.
        "Superhero", //	Featuring characters with superpowers or exceptional skills, often fighting against evil.
        "Lovecraftian" // Inspired by H.P. Lovecraft, featuring cosmic horror and the insignificance of humanity.
    };

    private static readonly string[] Ecosystems = {
        "Temperate Garden World", // Planets with a mild climate and abundant plant life, often considered ideal for colonization.
        "Wet Garden World", // Similar to temperate garden worlds but with a large percentage of the surface covered by water.
        "Desert World", // Dry planets with little to no water bodies, featuring arid landscapes like dunes and canyons.
        "Frozen Ice World", // Planets covered in ice and snow, with extremely cold temperatures.
        "Gas Giant", // Huge planets made primarily of gases, with no solid surface.
        "Volcanic World", // Planets characterized by frequent volcanic activity, possibly with rivers of lava.
        "Oceanic World", // Planets where the surface is almost entirely covered by deep oceans.
        "Jungle World", // Lush, humid planets covered in thick jungles and rainforests.
        "Barren World", // Planets with minimal signs of life, often lacking an atmosphere and featuring craters and rocks.
        "Toxic World", // Planets with a toxic atmosphere and/or surface, unsuitable for most forms of life.
        "Tidally Locked World", // Planets that show the same face to their star, resulting in one side in perpetual daylight.
        "Terraformed World", // Planets that have been artificially altered to support life, possibly resembling garden worlds.
        "Airless Moon", // Small, rocky bodies with little to no atmosphere, often orbiting larger planets.
        "Cloud World", // Planets with thick, perpetual cloud cover, often causing unusual weather patterns.
        "Storm World" // Planets characterized by extreme and constant storms, such as hurricanes or sandstorms.
    };
    // ReSharper restore StringLiteralTypo
}