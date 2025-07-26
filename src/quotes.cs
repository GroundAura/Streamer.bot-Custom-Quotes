using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class CPHInline
{
	public bool Execute()
	{
		// your main code goes here
		CPH.LogDebug("Quote Script :: Initialized");

		// Get which command was used to trigger the script
		var cmdArgs = GetActionArgs(cmdArgs: true);
		if (cmdArgs == null)
		{
			CPH.LogDebug("Quote Script :: Result :: Failed. Could not get command arguments.");
			return true;
		}
		string command = cmdArgs["command"];
		string input0 = cmdArgs["input0"];

		// If no command was found, cancel the script
		if (string.IsNullOrEmpty(command))
		{
			CPH.LogDebug("Quote Script :: Result :: Failed. Could not get command arguments.");
			return true;
		}

		// Check which command was used
		if (command.ToLower() == "!quote")
		{
			// If command was `!quote` check what the next input is
			if (string.IsNullOrEmpty(input0))
			{
				// If no next input, get a random quote
				QuoteGetRandom();
			}
			//else if (Regex.IsMatch(input0, @"^[0-9]+$"))
			//{
			//	// If the next input is a number, get the quote with that id
			//	QuoteGetId();
			//}
			else
			{
				switch (input0.ToLower())
				{
					// If the next input is part of a specific command, cancel the script and let the other triggered instance of the script handle it
					case "add":
					case "delete":
					case "edit":
					//case "find":
					case "get":
					case "hide":
					case "random":
					case "search":
						CPH.LogDebug("Quote Script :: Result :: Cancelled. Duplicate script instance detected.");
						return true;
					default:
						// If the next input is not part of a specific command, get a quote with that search term
						QuoteGetSearch();
						break;
				}
			}
		}
		else
		{
			// If command was not `!quote`, check if it is a specific command
			switch (command.ToLower())
			{
				case "!addquote":
				case "!quoteadd":
				case "!quote add":
					// If command was 'add quote', attempt to add the quote
					QuoteAdd();
					break;
				case "!delquote":
				case "!quotedelete":
				case "!quote delete":
					// If command was 'delete quote', attempt to delete the quote
					QuoteDelete();
					break;
				case "!editquote":
				case "!quoteedit":
				case "!quote edit":
					// If command was 'edit quote', attempt to edit the quote
					QuoteEdit();
					break;
				case "!getquote":
				case "!quoteget":
				case "!quote get":
				case "!searchquote":
				case "!quotesearch":
				case "!quote search":
					// If command was 'get quote by search term', attempt to get a quote
					QuoteGetSearch();
					break;
				case "!randquote":
				case "!quoterandom":
				case "!quote random":
					// If command was 'get random quote', attempt to get a random quote
					QuoteGetRandom();
					break;
				case "!hidequote":
				case "!quotehide":
				case "!quote hide":
					// If command was 'hide quote', attempt to hide the quote
					QuoteHide();
					break;
			}
		}

		CPH.LogDebug("Quote Script :: Result :: Success");
		return true;
	}

	//////////////////////
	// HELPER FUNCTIONS //
	//////////////////////

	/// <summary>
	/// Represents a quote entry and its properties.
	/// </summary>
	public class QuoteEntry
	{
		public string Id { get; set; }
		public string SpeakerName { get; set; }
		public string SpeakerId { get; set; }
		public string QuoteText { get; set; }
		public string ScribeName { get; set; }
		public string ScribeId { get; set; }
		public string DateTime { get; set; }
		public string Timestamp { get; set; }
		public string CategoryName { get; set; }
		public string CategoryId { get; set; }
		public string StreamTitle { get; set; }
		public string StreamPlatform { get; set; }
	}


	/// <summary>
	/// Appends a new quote to a JSON file.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	/// <param name="newQuote">The new quote to append.</param>
	public void AppendQuote(string filePath, QuoteEntry newQuote)
	{
		if (newQuote == null)
		{
			//CPH.LogDebug("Quote Script :: AppendQuote() :: New quote is null");
			return;
		}

		//CPH.LogDebug("Quote Script :: AppendQuote() :: Reading quotes from file");
		List<QuoteEntry> quotes = ReadQuotes(filePath);
		if (quotes == null)
		{
			//CPH.LogDebug("Quote Script :: AppendQuote() :: `quotes` is null");
			return;
		}

		//CPH.LogDebug("Quote Script :: AppendQuote() :: Adding quote to list");
		quotes.Add(newQuote);

		//CPH.LogDebug("Quote Script :: AppendQuote() :: Writing quotes to file");
		WriteQuotes(filePath, quotes);
	}


	/// <summary>
	/// Reads quotes from a JSON file.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	/// <returns>A list of quote entries.</returns>
	public static List<QuoteEntry> ReadQuotes(string filePath)
	{
		if (File.Exists(filePath))
		{
			string json = File.ReadAllText(filePath);
			var quotes = JsonConvert.DeserializeObject<List<QuoteEntry>>(json);
			if (quotes == null || quotes.Count == 0)
			{
				return new List<QuoteEntry>(); // Return an empty list if the file is empty
			}
			return quotes;
		}
		else
		{
			return new List<QuoteEntry>(); // Return an empty list if the file does not exist
		}
	}


	/// <summary>
	/// Writes quotes to a JSON file.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	/// <param name="quotes">The list of quotes to write.</param>
	public static void WriteQuotes(string filePath, List<QuoteEntry> quotes)
	{
		string json = JsonConvert.SerializeObject(quotes, Formatting.Indented);
		File.WriteAllText(filePath, json);
	}


	/// <summary>
	/// Gets the latest quote (based on Id) from a list of quotes.
	/// </summary>
	/// <param name="quotes">The list of quotes.</param>
	/// <returns>The latest quote entry, or null if the list is null or empty.</returns>
	public static QuoteEntry GetLatestQuote(List<QuoteEntry> quotes)
	{
		if (quotes == null || quotes.Count == 0)
		{
			return null; // Return null if the list is null or empty
		}
		return quotes
			.Select(q => new { Quote = q, Id = int.Parse(q.Id) }) // Convert Id to int for comparison
			.OrderByDescending(q => q.Id) // Sort by Id in descending order
			.Select(q => q.Quote) // Select the original QuoteEntry
			.FirstOrDefault(); // Get the first (latest) entry or null if the list is empty
	}


	/// <summary>
	/// Returns a dictionary of relevant arguments for the action.
	/// </summary>
	/// <returns>A dictionary of relevant arguments or null.</returns>
	public Dictionary<string, string> GetActionArgs(
		bool cmdArgs = false,
		bool dbArg = false,
		bool inputArgs = false,
		bool streamArgs = false,
		bool userArgs = false
	)
	{
		//CPH.LogDebug("Quote Script :: GetActionArgs() :: Getting arguments");
		var args = new Dictionary<string, string>();

		// File path
		if (dbArg)
		{
			CPH.TryGetArg("quoteDatabasePath", out string quoteDatabasePath);
			args.Add("quoteDatabasePath", quoteDatabasePath);
		}

		// Broadcast info
		CPH.TryGetArg("broadcastUser", out string broadcastUser);
		CPH.TryGetArg("broadcastUserId", out string broadcastUserId);
		if (streamArgs)
		{
			args.Add("broadcastUser", broadcastUser);
			args.Add("broadcastUserId", broadcastUserId);
			CPH.TryGetArg("broadcastUserName", out string broadcastUserName);
			args.Add("broadcastUserName", broadcastUserName);
			CPH.TryGetArg("twitchChannelCategoryId", out string twitchChannelCategoryId);
			args.Add("twitchChannelCategoryId", twitchChannelCategoryId);
			CPH.TryGetArg("twitchChannelCategoryName", out string twitchChannelCategoryName);
			args.Add("twitchChannelCategoryName", twitchChannelCategoryName);
			CPH.TryGetArg("twitchChannelTitle", out string twitchChannelTitle);
			args.Add("twitchChannelTitle", twitchChannelTitle);
		}

		// User info (redeemer)
		if (userArgs)
		{
			CPH.TryGetArg("user", out string user);
			args.Add("user", user);
			CPH.TryGetArg("userName", out string userName);
			args.Add("userName", userName);
			CPH.TryGetArg("userId", out string userId);
			args.Add("userId", userId);
			CPH.TryGetArg("userType", out string userType);
			args.Add("userType", userType);
			CPH.TryGetArg("isModerator", out string isModerator);
			args.Add("isModerator", isModerator);
			CPH.TryGetArg("isVip", out string isVip);
			args.Add("isVip", isVip);
		}

		// Command
		CPH.TryGetArg("command", out string command);
		if (cmdArgs)
		{
			args.Add("command", command);
			CPH.TryGetArg("input0", out string input0);
			args.Add("input0", input0);
		}

		// Input
		if (inputArgs)
		{
			//CPH.LogDebug("Quote Script :: GetActionArgs() :: Handling input arguments");
			CPH.TryGetArg("rawInput", out string rawInput);
			args.Add("rawInput", rawInput);
			CPH.TryGetArg("actionQueuedAt", out string actionQueuedAt);
			args.Add("actionQueuedAt", actionQueuedAt);

			CPH.TryGetArg("targetUser", out string targetUser);
			args.Add("targetUser", targetUser);
			CPH.TryGetArg("targetUserName", out string targetUserName);
			args.Add("targetUserName", targetUserName);
			CPH.TryGetArg("targetUserId", out string targetUserId);
			args.Add("targetUserId", targetUserId);
			CPH.TryGetArg("targetUserPlatform", out string targetUserPlatform);
			args.Add("targetUserPlatform", targetUserPlatform);
			CPH.TryGetArg("targetIsFollowing", out string targetIsFollowing);
			//args.Add("targetIsFollowing", targetIsFollowing);
			CPH.TryGetArg("targetIsModerator", out string targetIsModerator);
			//args.Add("targetIsModerator", targetIsModerator);
			CPH.TryGetArg("targetLastActive", out string targetLastActive);
			//args.Add("targetLastActive", targetLastActive);

			// Get full input message
			string rawInputMessage = string.Join(" ", command, rawInput);
			args.Add("rawInputMessage", rawInputMessage);

			// Split input into target and content
			string inputContent = null;
			string inputTarget = null;
			string inputTargetId = null;
			string inputTargetOperator = null;
			if (!string.IsNullOrWhiteSpace(rawInput))
			{
				string[] inputSplit = rawInput.Split(' ');
				// Check if input is 2 or more words
				if (inputSplit.Length > 1)
				{
					inputTarget = inputSplit[0];
					// Check if there's a mention (@) or character (^) operator, remove it, and set the operator
					if (inputTarget[0] == '^')
					{
						// Target is explicitly a character, not a user
						inputTargetOperator = "^";
						inputTarget = inputTarget.Substring(1);
						inputContent = string.Join(" ", inputSplit.Skip(1));
					}
					else if (inputTarget[0] == '@')
					{
						// Target is explicitly the mentioned user
						inputTargetOperator = "@";
						inputTarget = targetUser;
						inputTargetId = targetUserId;
						inputContent = string.Join(" ", inputSplit.Skip(1));
					}
					else
					{
						// Attempt to determine if target is a user or not
						// Check if the input target matches any of the broadcaster's aliases/nicknames
						CPH.TryGetArg("twitchBroadcasterAliases", out string twitchBroadcasterAliases);
						string[] aliases = twitchBroadcasterAliases.Split(',');
						if (Array.Exists(aliases, alias => string.Equals(alias, inputTarget, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(broadcastUser))
						{
							// Target is most likely the broadcaster
							inputTarget = broadcastUser;
							inputTargetId = broadcastUserId;
							inputContent = string.Join(" ", inputSplit.Skip(1));
						}
						// Check if the input target matches the user found by Streamer.bot
						else if (inputTarget.ToLower() == targetUser.ToLower() || inputTarget.ToLower() == targetUserName.ToLower())
						{
							// Check if the matched user is following or has ever been active in the stream
							string defaultLastActive = "1/1/0001 12:00:00 AM";
							if (targetIsFollowing == "True" || targetIsModerator == "True" || targetLastActive != defaultLastActive && !string.IsNullOrEmpty(targetLastActive))
							{
								// Target is most likely the matched user
								inputTarget = targetUser;
								inputTargetId = targetUserId;
								inputContent = string.Join(" ", inputSplit.Skip(1));
							}
							else
							{
								// Target word is most likely not a user, and instead part of the content
								inputTarget = null;
								inputContent = string.Join(" ", inputSplit);
							}
						}
						else
						{
							// Target word is most likely not a user, and instead part of the content
							inputTarget = null;
							inputContent = string.Join(" ", inputSplit);
						}
					}
				}
				else
				{
					// Target word is most likely not a user, and instead part of the content
					inputTarget = null;
					inputContent = rawInput;
				}
			}
			args.Add("inputTarget", inputTarget);
			args.Add("inputTargetId", inputTargetId);
			args.Add("inputTargetOperator", inputTargetOperator);
			args.Add("inputContent", inputContent);

		}

		if (args.Count == 0)
		{
			return null;
		}
		return args;
	}


	/// <summary>
	/// Adds a new quote to the quote database.
	/// </summary>
	public void QuoteAdd()
	{
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Beginning");

		CPH.SendMessage("Command Triggered: Quote (Add)", true, true);

		var args = GetActionArgs(dbArg: true, inputArgs: true, streamArgs: true, userArgs: true);
		string newId;
		string newSpeakerName;
		string newSpeakerId;
		string newQuoteText;
		string newScribeName;
		string newScribeId;
		string newDateTime;
		string newTimestamp;
		string newCategoryName;
		string newCategoryId;
		string newStreamTitle;
		string newStreamPlatform;

		// Id
		// read file and get last id
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Reading quotes from file");
		var quotes = ReadQuotes(args["quoteDatabasePath"]);
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Getting latest quote id");
		var latestQuote = GetLatestQuote(quotes);
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Setting quote id");
		// if no last id, set id to 1
		if (latestQuote == null || string.IsNullOrEmpty(latestQuote.Id))
		{
			newId = "1";
			CPH.LogDebug("Quote Script :: QuoteAdd() :: No existing quotes, set quote id to 1");
		}
		// set id to last id + 1
		else
		{
			try
			{
				newId = (int.Parse(latestQuote.Id) + 1).ToString();
				//CPH.LogDebug("Quote Script :: QuoteAdd() :: Set quote id to " + newId);
			}
			catch
			{
				newId = "1";
				CPH.LogDebug("Quote Script :: QuoteAdd() :: Exception caught, set quote id to 1");
			}
		}

		// SpeakerName & SpeakerId
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Setting Speaker");
		newSpeakerName = args["inputTarget"];
		newSpeakerId = args["inputTargetId"];

		// QuoteText
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Quote Text");
		newQuoteText = args["inputContent"];

		// ScribeName & ScribeId
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Setting Scribe");
		if (string.IsNullOrEmpty(args["user"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'user' does not exist");
			newScribeName = null;
		}
		else
		{
			newScribeName = args["user"];
		}

		if (string.IsNullOrEmpty(args["userId"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'userId' does not exist");
			newScribeId = null;
		}
		else
		{
			newScribeId = args["userId"];
		}

		// DateTime
		//CPH.LogDebug("Quote Script :: Setting DateTime");
		//newDateTime = DateTime.Now.ToString("o"); // ISO 8601 format
		if (string.IsNullOrEmpty(args["actionQueuedAt"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'actionQueuedAt' does not exist");
			newDateTime = null;
		}
		else
		{
			newDateTime = args["actionQueuedAt"];
		}

		// Timestamp
		// get vod
		// get uptime
		newTimestamp = null;

		// CategoryName & CategoryId
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Setting Category");
		if (string.IsNullOrEmpty(args["twitchChannelCategoryName"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'game' does not exist");
			newCategoryName = null;
		}
		else
		{
			newCategoryName = args["twitchChannelCategoryName"];
		}

		if (string.IsNullOrEmpty(args["twitchChannelCategoryId"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'gameId' does not exist");
			newCategoryId = null;
		}
		else
		{
			newCategoryId = args["twitchChannelCategoryId"];
		}

		// StreamTitle
		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Setting Title");
		if (string.IsNullOrEmpty(args["twitchChannelTitle"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'twitchChannelTitle' does not exist");
			newStreamTitle = null;
		}
		else
		{
			newStreamTitle = args["twitchChannelTitle"];
		}

		// StreamPlatform
		if (string.IsNullOrEmpty(args["userType"]))
		{
			CPH.LogDebug("Quote Script :: QuoteAdd() :: arg 'userType' does not exist");
			newStreamPlatform = null;
		}
		else
		{
			newStreamPlatform = args["userType"];
		}

		var newQuote = new QuoteEntry
		{
			Id = newId,
			SpeakerName = newSpeakerName,
			SpeakerId = newSpeakerId,
			QuoteText = newQuoteText,
			ScribeName = newScribeName,
			ScribeId = newScribeId,
			DateTime = newDateTime,
			Timestamp = newTimestamp,
			CategoryName = newCategoryName,
			CategoryId = newCategoryId,
			StreamTitle = newStreamTitle,
			StreamPlatform = newStreamPlatform
		};

		//CPH.LogDebug("Quote Script :: QuoteAdd() :: Appending quote to file");
		AppendQuote(args["quoteDatabasePath"], newQuote);
	}


	/// <summary>
	/// Removes a quote from the quote database.
	/// </summary>
	public void QuoteDelete()
	{
		CPH.SendMessage("Command Triggered: Quote (Delete)", true, true);
	}


	/// <summary>
	/// Edits a quote in the quote database.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	public void QuoteEdit()
	{
		CPH.SendMessage("Command Triggered: Quote (Edit)", true, true);
	}


	///// <summary>
	///// Gets a quote from the quote database by Id.
	///// </summary>
	///// <param name="filePath">The path to the JSON file.</param>
	//public void QuoteGetId()
	//{
	//	CPH.SendMessage("Command Triggered: Quote (Get from Id)", true, true);
	//}


	/// <summary>
	/// Gets a random quote from the quote database.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	public void QuoteGetRandom()
	{
		CPH.SendMessage("Command Triggered: Quote (Get Random)", true, true);
	}


	/// <summary>
	/// Gets a quote from the quote database by search term.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	public void QuoteGetSearch()
	{
		CPH.SendMessage("Command Triggered: Quote (Get from Search Term)", true, true);
	}


	/// <summary>
	/// Hides a quote from the quote database.
	/// </summary>
	/// <param name="filePath">The path to the JSON file.</param>
	public void QuoteHide()
	{
		CPH.SendMessage("Command Triggered: Quote (Hide)", true, true);
	}

}
