using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlashCards;

public abstract class Menu
{
    protected MenuManager MenuManager { get; }
    protected FlashcardDb FlashcardDb { get; }
    protected StackDb StackDb { get; }
    protected Menu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb)
    {
        MenuManager = menuManager;
        FlashcardDb = flashcardDb;
        StackDb = stackDb;
    }
    public abstract void Display();
}
public class MainMenu : Menu
{
    public MainMenu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb) : base(menuManager, flashcardDb, stackDb) { }

    public override void Display()
    {
        UserInterface.MainMenu();

        switch (UserInterface.OptionChoice)
        {
            case "New Study Session":
                MenuManager.NewMenu(new StudySessionMenu(MenuManager, FlashcardDb, StackDb));
                break;
            case "New Flashcard":
                MenuManager.NewMenu(new FlashCardMenu(MenuManager, FlashcardDb, StackDb));
                break;
            case "Show Stacks":
                MenuManager.NewMenu(new ShowStacksMenu(MenuManager, FlashcardDb, StackDb));
                break;
            case "Show Study Sessions":
                break;
            default:
                Environment.Exit(0);
                break;
        }
    }
}

public class StudySessionMenu : Menu
{
    public StudySessionMenu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb) : base(menuManager, flashcardDb, stackDb) { }

    public override void Display()
    {
        UserInterface.StudySession();
    }
}

public class FlashCardMenu : Menu
{
    protected List<Stack> stacksList;
    public FlashCardMenu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb) : base(menuManager, flashcardDb, stackDb) { }

    public override void Display()
    {
        stacksList = StackDb.GetAll();

        if (stacksList.Count != 0)
        {
            DisplayStackOptions();

            try
            {
                HandleUserOptions();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                UserInput.DisplayMessage();
                MenuManager.GoBack();
            }
        }
        else
        {
            var newStack = CreateStack();
            HandleFlashcardCreation(newStack);
        }
    }

    private void DisplayStackOptions()
    {
        var stacksArray = GetStackNamesArray(stacksList);
        UserInterface.NewFlashcard(stacksArray);
    }

    private void HandleUserOptions()
    {
        Stack userStack;

        switch (UserInterface.OptionChoice)
        {
            case "Create a new stack":
                userStack = CreateStack();
                HandleFlashcardCreation(userStack);
                break;
            case "Go back":
                MenuManager.GoBack();
                break;
            default:
                userStack = stacksList.FirstOrDefault(s => s.Name == UserInterface.OptionChoice);
                if (userStack == null)
                {
                    throw new InvalidOperationException($"A stack with the name {UserInterface.OptionChoice}, does not exist in this context.");
                }
                HandleFlashcardCreation(userStack);
                break;
        }
    }

    private void HandleFlashcardCreation(Stack currentStack)
    {
        var anotherFlashcard = true;

        while (anotherFlashcard)
        {
            UserInterface.NewFlashcardQuestion(currentStack.Name);
            var userQuestion = UserInput.InputWithSpecialKeys(MenuManager, true, 50);

            UserInterface.NewFlashcardAnswer(currentStack.Name, userQuestion);
            var userAnswer = UserInput.InputWithSpecialKeys(MenuManager, true, 50);

            UserInterface.NewFlashcardConfirm(currentStack.Name, userQuestion, userAnswer);
            if (UserInterface.OptionChoice == "Confirm")
            {
                FlashcardDb.Insert(userQuestion, userAnswer, currentStack.Id);
                UserInterface.AnotherFlashcard();
                if (UserInterface.OptionChoice == "Done")
                {
                    anotherFlashcard = false;
                }
            }
        }
        MenuManager.DisplayCurrentMenu();
    }

    protected static string[] GetStackNamesArray(List<Stack> stacks)
    {
        var stacksArray = new string[stacks.Count];
        for (int i = 0; i < stacks.Count; i++)
        {
            stacksArray[i] = stacks[i].Name;
        }
        return stacksArray;
    }

    private Stack CreateStack()
    {
        string stackName = HandleStackNameInput();

        StackDb.Insert(stackName.ToLower());

        return StackDb.GetByName(stackName);
    }

    private string HandleStackNameInput()
    {
        string stackName;
        do
        {
            UserInterface.NewStack();
            stackName = UserInput.InputWithSpecialKeys(MenuManager, true, 50).ToLower();
            if (StackDb.NamePresent(stackName))
            {
                UserInput.DisplayMessage($"Stack {stackName} already exists.", "enter again");
            }
        } while (StackDb.NamePresent(stackName));

        return stackName;
    }
}

public class ShowStacksMenu : FlashCardMenu
{
    private List<Flashcard> flashcards;
    public ShowStacksMenu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb) : base(menuManager, flashcardDb, stackDb) { }

    public override void Display()
    {
        stacksList = StackDb.GetAll();

        if (stacksList.Count != 0)
        {
            DisplayStackOptions();

            try
            {
                HandleUserOptions();
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                UserInput.DisplayMessage();
                MenuManager.GoBack();
            }
            HandleActionMenu();
        }
        else
        {
            UserInput.DisplayMessage("No flashcards yet.", "return to Main Menu", true);
            MenuManager.ReturnToMainMenu();
        }
    }

    private void DisplayStackOptions()
    {
        var stacksArray = GetStackNamesArray(stacksList);
        UserInterface.ShowStacks(stacksArray);
    }

    private void DisplayFlashcards(Stack stack)
    {
        List<FlashcardReviewDto> flashcardDtos = ConvertToDto(flashcards);
        UserInterface.ShowFlashcards(flashcardDtos, stack);
    }
    private void DisplayFlashcards(List<Stack> stacks)
    {
        List<FlashcardReviewDto> flashcardDtos = ConvertToDto(flashcards);
        UserInterface.ShowFlashcards(flashcardDtos, stacks);
    }

    private void HandleUserOptions()
    {
        Stack userStack;

        switch (UserInterface.OptionChoice)
        {
            case "Show all":
                flashcards = FlashcardDb.GetAll();
                DisplayFlashcards(stacksList);
                break;

            case "Go back":
                MenuManager.GoBack();
                break;

            default:
                userStack = stacksList.FirstOrDefault(s => s.Name == UserInterface.OptionChoice);
                if (userStack == null)
                {
                    throw new InvalidOperationException($"A stack with the name {UserInterface.OptionChoice}, does not exist in this context.");
                }
                else
                {
                    flashcards = FlashcardDb.GetByStackId(userStack.Id);
                    DisplayFlashcards(userStack);
                }
                break;
        }
    }

    private void HandleActionMenu()
    {
        switch (UserInterface.OptionChoice)
        {
            case "Go back":
                MenuManager.GoBack();
                break;
            case "Update a Flashcard":
                break;
            case "Delete a Flashcard":
                break;
            case "Delete a Stack":
                break;
        }
    }

    private List<FlashcardReviewDto> ConvertToDto(List<Flashcard> flashcards)
    {
        var flashcardDtos = new List<FlashcardReviewDto>();
        int startId = 1;
        foreach (var flashcard in flashcards)
        {
            flashcardDtos.Add(new FlashcardReviewDto
            {
                DisplayId = startId++,
                Question = flashcard.Question,
                Answer = flashcard.Answer,
                StackId = flashcard.StackId
            });
        }
        return flashcardDtos;
    }
}

public class ShowStudySessionsMenu : Menu
{
    public ShowStudySessionsMenu(MenuManager menuManager, FlashcardDb flashcardDb, StackDb stackDb) : base(menuManager, flashcardDb, stackDb) { }

    public override void Display()
    {
        UserInterface.ShowStudySessions();
    }
}