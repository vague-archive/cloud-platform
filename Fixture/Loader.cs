namespace Void.Platform.Fixture;

public static class Loader
{
  public static void Load(ILogger logger, string databaseUrl)
  {
    using (var db = new Database(logger, databaseUrl))
    {
      var factory = new FixtureFactory(db);
      var orgs = LoadOrganizations(factory);
      var users = LoadUsers(factory);
      var roles = LoadUserRoles(factory, users);
      var identities = LoadIdentities(factory, users);
      var members = LoadMembers(factory, users, orgs);
      var tokens = LoadTokens(factory, users, orgs);
      var games = LoadGames(factory, orgs);
    }
  }

  //-----------------------------------------------------------------------------------------------

  private class Index<T> : Dictionary<string, T> { }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.Organization> LoadOrganizations(FixtureFactory factory)
  {
    var index = new Index<Account.Organization>();

    index.Add("void", factory.CreateOrganization(
      id: "void",
      name: "Void",
      slug: "void"
    ));

    index.Add("atari", factory.CreateOrganization(
      id: "atari",
      name: "Atari",
      slug: "atari"
    ));

    index.Add("nintendo", factory.CreateOrganization(
      id: "nintendo",
      name: "Nintendo",
      slug: "nintendo"
    ));

    index.Add("secret", factory.CreateOrganization(
      id: "secret",
      name: "Secret",
      slug: "secret"
    ));

    index.Add("aardvark", factory.CreateOrganization(
      id: "aardvark",
      name: "Aardvark Inc",
      slug: "aardvark"
    ));

    index.Add("zoidberg", factory.CreateOrganization(
      id: "zoidberg",
      name: "Zoidberg Inc",
      slug: "zoidberg"
    ));

    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.User> LoadUsers(FixtureFactory factory)
  {
    var index = new Index<Account.User>();

    index.Add("active", factory.CreateUser(
      id: "active",
      name: "Active User",
      email: "active@example.com",
      timezone: "America/Los_Angeles",
      locale: "en-US",
      password: "password",
      disabled: false
    ));

    index.Add("other", factory.CreateUser(
      id: "other",
      name: "Other User",
      email: "other@example.com",
      timezone: "America/New_York",
      locale: "en-US",
      password: "password",
      disabled: false
    ));

    index.Add("disabled", factory.CreateUser(
      id: "disabled",
      name: "Disabled User",
      email: "disabled@example.com",
      timezone: "America/Los_Angeles",
      locale: "en-US",
      password: "password",
      disabled: true
    ));

    index.Add("sysadmin", factory.CreateUser(
      id: "sysadmin",
      name: "Sysadmin User",
      email: "sysadmin@example.com",
      timezone: "America/Los_Angeles",
      locale: "en-US",
      password: "password",
      disabled: false
    ));

    index.Add("floater", factory.CreateUser(
      id: "floater",
      name: "The Floater",
      email: "floater@unknown.com",
      disabled: false,
      password: "password",
      timezone: "Europe/Paris",
      locale: "en-US"
    ));

    index.Add("outsider", factory.CreateUser(
      id: "outsider",
      name: "The Outsider",
      email: "outsider@unknown.com",
      disabled: false,
      password: "password",
      timezone: "Etc/UTC",
      locale: "en-GB"
    ));

    index.Add("bushnell", factory.CreateUser(
      id: "bushnell",
      name: "Nolan Bushnell",
      email: "bushnell@atari.com",
      disabled: false,
      password: "p0ng!",
      timezone: "America/Los_Angeles",
      locale: "en-US"
    ));

    index.Add("miyamoto", factory.CreateUser(
      id: "miyamoto",
      name: "Shigeru Miyamoto",
      email: "miyamoto@nintendo.com",
      disabled: false,
      password: "mar10!",
      timezone: "Asia/Tokyo",
      locale: "en-US"
    ));

    index.Add("jake", factory.CreateUser(
      id: "jake",
      name: "Jake Gordon",
      email: "jake@void.dev",
      disabled: false,
      password: "password",
      timezone: "America/Los_Angeles",
      locale: "en-US"
    ));

    index.Add("scarlett", factory.CreateUser(
      id: "scarlett",
      name: "Scarlett Blaiddyd",
      email: "scarlett@void.dev",
      disabled: false,
      password: "password",
      timezone: "America/Los_Angeles",
      locale: "en-US"
    ));

    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.UserRole[]> LoadUserRoles(FixtureFactory factory, Index<Account.User> users)
  {
    var index = new Index<Account.UserRole[]>();

    index.Add("sysadmin:sysadmin", [
      factory.CreateUserRole(
        user: users["sysadmin"],
        role: Account.UserRole.SysAdmin
      )
    ]);

    index.Add("jake:sysadmin", [
      factory.CreateUserRole(
        user: users["jake"],
        role: Account.UserRole.SysAdmin
      )
    ]);

    index.Add("scarlett:sysadmin", [
      factory.CreateUserRole(
        user: users["scarlett"],
        role: Account.UserRole.SysAdmin
      )
    ]);

    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.Identity> LoadIdentities(FixtureFactory factory, Index<Account.User> users)
  {
    var index = new Index<Account.Identity>();

    index.Add("active:github", factory.CreateIdentity(
      id: "active:github",
      user: users["active"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "active",
      username: "active"
    ));

    index.Add("other:github", factory.CreateIdentity(
      id: "other:github",
      user: users["other"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "other",
      username: "other"
    ));

    index.Add("disabled:github", factory.CreateIdentity(
      id: "disabled:github",
      user: users["disabled"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "disabled",
      username: "disabled"
    ));

    index.Add("sysadmin:github", factory.CreateIdentity(
      id: "sysadmin:github",
      user: users["sysadmin"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "sysadmin",
      username: "sysadmin"
    ));

    index.Add("floater:github", factory.CreateIdentity(
      id: "floater:github",
      user: users["floater"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "floater",
      username: "floater"
    ));

    index.Add("outsider:github", factory.CreateIdentity(
      id: "outsider:github",
      user: users["outsider"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "outsider",
      username: "outsider"
    ));

    index.Add("bushnell:github", factory.CreateIdentity(
      id: "bushnell:github",
      user: users["bushnell"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "bushnell",
      username: "bushnell"
    ));

    index.Add("miyamoto:github", factory.CreateIdentity(
      id: "miyamoto:github",
      user: users["miyamoto"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "miyamoto",
      username: "miyamoto"
    ));

    index.Add("jake:github", factory.CreateIdentity(
      id: "jake:github",
      user: users["jake"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "738109",
      username: "jakesgordon"
    ));

    index.Add("jake:discord", factory.CreateIdentity(
      id: "jake:discord",
      user: users["jake"],
      provider: Account.IdentityProvider.Discord,
      identifier: "1204083812892418108",
      username: "jakesgordon"
    ));

    index.Add("scarlett", factory.CreateIdentity(
      id: "scarlett:github",
      user: users["scarlett"],
      provider: Account.IdentityProvider.GitHub,
      identifier: "46874110",
      username: "scarlettblaiddyd"
    ));

    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.Member> LoadMembers(FixtureFactory factory, Index<Account.User> users, Index<Account.Organization> orgs)
  {
    var index = new Index<Account.Member>();

    index.Add("void:jake", factory.CreateMember(orgs["void"], users["jake"]));
    index.Add("void:scarlett", factory.CreateMember(orgs["void"], users["scarlett"]));
    index.Add("void:floater", factory.CreateMember(orgs["void"], users["floater"]));
    index.Add("atari:active", factory.CreateMember(orgs["atari"], users["active"]));
    index.Add("atari:disabled", factory.CreateMember(orgs["atari"], users["disabled"]));
    index.Add("atari:sysadmin", factory.CreateMember(orgs["atari"], users["sysadmin"]));
    index.Add("atari:bushnell", factory.CreateMember(orgs["atari"], users["bushnell"]));
    index.Add("atari:floater", factory.CreateMember(orgs["atari"], users["floater"]));
    index.Add("atari:jake", factory.CreateMember(orgs["atari"], users["jake"]));
    index.Add("atari:scarlett", factory.CreateMember(orgs["atari"], users["scarlett"]));
    index.Add("nintendo:miyamoto", factory.CreateMember(orgs["nintendo"], users["miyamoto"]));
    index.Add("nintendo:floater", factory.CreateMember(orgs["nintendo"], users["floater"]));
    index.Add("nintendo:jake", factory.CreateMember(orgs["nintendo"], users["jake"]));
    index.Add("nintendo:scarlett", factory.CreateMember(orgs["nintendo"], users["scarlett"]));
    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.Token> LoadTokens(FixtureFactory factory, Index<Account.User> users, Index<Account.Organization> orgs)
  {
    var index = new Index<Account.Token>();

#pragma warning disable format
    index.Add("active",   factory.CreateToken(id: "active",   value: "active",   user: users["active"],   type: Account.TokenType.Access));
    index.Add("other",    factory.CreateToken(id: "other",    value: "other",    user: users["other"],    type: Account.TokenType.Access));
    index.Add("disabled", factory.CreateToken(id: "disabled", value: "disabled", user: users["disabled"], type: Account.TokenType.Access));
    index.Add("sysadmin", factory.CreateToken(id: "sysadmin", value: "sysadmin", user: users["sysadmin"], type: Account.TokenType.Access));
    index.Add("floater",  factory.CreateToken(id: "floater",  value: "floater",  user: users["floater"],  type: Account.TokenType.Access));
    index.Add("outsider", factory.CreateToken(id: "outsider", value: "outsider", user: users["outsider"], type: Account.TokenType.Access));
    index.Add("bushnell", factory.CreateToken(id: "bushnell", value: "bushnell", user: users["bushnell"], type: Account.TokenType.Access));
    index.Add("miyamoto", factory.CreateToken(id: "miyamoto", value: "miyamoto", user: users["miyamoto"], type: Account.TokenType.Access));
    index.Add("jake",     factory.CreateToken(id: "jake",     value: "jake",     user: users["jake"],     type: Account.TokenType.Access));
    index.Add("scarlett", factory.CreateToken(id: "scarlett", value: "scarlett", user: users["scarlett"], type: Account.TokenType.Access));
#pragma warning restore format

    index.Add("jake:legacy", factory.CreateToken(
      id: "jake:legacy",
      value: Crypto.Base64Encode("jake"), // VOID_ACCESS_TOKEN=amFrZQ==
      user: users["jake"],
      type: Account.TokenType.Access
    ));

    index.Add("scarlett:legacy", factory.CreateToken(
      id: "scarlett:legacy",
      value: Crypto.Base64Encode("scarlett"), // VOID_ACCESS_TOKEN=c2NhcmxldHQ=
      user: users["scarlett"],
      type: Account.TokenType.Access
    ));

    var atari = orgs["atari"];

    index.Add("invite", factory.CreateToken(
      id: "invite",
      value: "invite",
      type: Account.TokenType.Invite,
      org: atari,
      sentTo: "pending@member.com",
      expiresOn: factory.Now.Plus(Account.TokenTTL(Account.TokenType.Invite))
    ));

    index.Add("invite:contractor", factory.CreateToken(
      id: "invite:contractor",
      value: "invite:contractor",
      type: Account.TokenType.Invite,
      org: atari,
      sentTo: "contractor@agency.com",
      expiresOn: factory.Now.Plus(Account.TokenTTL(Account.TokenType.Invite))
    ));

    return index;
  }

  //-----------------------------------------------------------------------------------------------

  private static Index<Account.Game> LoadGames(FixtureFactory factory, Index<Account.Organization> orgs)
  {
    var index = new Index<Account.Game>();

    index.Add("snakes", factory.CreateGame(
      id: "snakes",
      org: orgs["void"],
      name: "Snakes",
      slug: "snakes",
      description: "A game where players control a growing snake that navigates around the screen, eating food to grow longer while avoiding collisions with the walls or its own tail."
    ));

    index.Add("tetris", factory.CreateGame(
      id: "tetris",
      org: orgs["void"],
      name: "Tetris",
      slug: "tetris",
      description: "A tile-matching puzzle game where players manipulate falling tetrominoes to create and clear horizontal lines without gaps, preventing the stack from reaching the top of the playfield."
    ));

    index.Add("tinyplatformer", factory.CreateGame(
      id: "tinyplatformer",
      org: orgs["void"],
      name: "Tiny Platformer",
      slug: "tiny-platformer",
      description: "A tiny platform game, collect the gold boxes, and squash the monsters"
    ));

    index.Add("bunner", factory.CreateGame(
      id: "bunner",
      org: orgs["void"],
      name: "Infinite Bunner",
      slug: "infinite-bunner",
      description: "Help the bunny cross the road. How far can you make it?"
    ));

    index.Add("share-tool", factory.CreateGame(
      id: "share-tool",
      org: orgs["void"],
      name: "Share Tool",
      slug: "share-tool",
      description: "A Fiasco Editor Tool for sharing your game",
      purpose: Account.GamePurpose.Tool
    ));

    index.Add("magic-tool", factory.CreateGame(
      id: "magic-tool",
      org: orgs["void"],
      name: "Magic Tool",
      slug: "magic-tool",
      description: "A Fiasco Editor Tool for adding magic to your game",
      purpose: Account.GamePurpose.Tool
    ));

    index.Add("archived-tool", factory.CreateGame(
      id: "archived-tool",
      org: orgs["void"],
      name: "Archived Tool",
      slug: "archived-tool",
      isArchived: true,
      description: "A Fiasco Editor Tool that has been archived",
      purpose: Account.GamePurpose.Tool
    ));

    index.Add("pong", factory.CreateGame(
      id: "pong",
      org: orgs["atari"],
      name: "Pong",
      slug: "pong",
      description: "A classic arcade game where two players control paddles to hit a ball back and forth across a screen, trying to score points by getting the ball past their opponent's paddle."
    ));

    index.Add("pitfall", factory.CreateGame(
      id: "pitfall",
      org: orgs["atari"],
      name: "Pitfall",
      slug: "pitfall",
      description: "A side-scrolling adventure game where players control Pitfall Harry as he navigates a jungle filled with obstacles and hazards, such as pits, crocodiles, and rolling logs, to collect treasures within a time limit."
    ));

    index.Add("asteroids", factory.CreateGame(
      id: "asteroids",
      org: orgs["atari"],
      name: "Asteroids",
      slug: "asteroids",
      description: "An arcade space shooter game where players control a spaceship that must destroy incoming asteroids and flying saucers while avoiding collisions."
    ));

    index.Add("et", factory.CreateGame(
      id: "et",
      org: orgs["atari"],
      name: "E.T. the Extra-Terrestrial",
      slug: "et",
      isArchived: true,
      description: "A game that has been archived because it was sooooo bad"
    ));

    index.Add("retro-tool", factory.CreateGame(
      id: "retro-tool",
      org: orgs["atari"],
      name: "Retro Tool",
      slug: "retro-tool",
      description: "A Fiasco Editor Tool for adding 8-bit retro style to your game",
      purpose: Account.GamePurpose.Tool
    ));

    index.Add("donkeykong", factory.CreateGame(
      id: "donkeykong",
      org: orgs["nintendo"],
      name: "Donkey Kong",
      slug: "donkey-kong",
      description: "A classic arcade game where players control Jumpman (later known as Mario) as he climbs ladders and avoids obstacles to rescue a damsel in distress from the giant ape, Donkey Kong."
    ));

    index.Add("star-tool", factory.CreateGame(
      id: "star-tool",
      org: orgs["nintendo"],
      name: "Star Tool",
      slug: "star-tool",
      description: "A Fiasco Editor Tool for adding collectible stars to your game",
      purpose: Account.GamePurpose.Tool
    ));

    index.Add("surprise", factory.CreateGame(
      id: "surprise",
      org: orgs["secret"],
      name: "Surprise Game",
      slug: "surprise",
      description: "A surprise game, in a secret organization, that nobody should every be able to see (used for auth testing)"
    ));

    return index;
  }

  //-----------------------------------------------------------------------------------------------
}

//-------------------------------------------------------------------------------------------------