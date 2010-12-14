﻿using System;
using System.ComponentModel.DataAnnotations;
using Csla;
using Csla.Serialization;

namespace BusinessLibrary
{
  [Serializable]
  [Csla.Server.ObjectFactory("DataAccess.OrderFactory, DataAccess")]
  public class Order : BusinessBase<Order>
  {
    public static PropertyInfo<int> IdProperty = RegisterProperty<int>(p => p.Id);
    public int Id
    {
      get { return GetProperty(IdProperty); }
      private set { SetProperty(IdProperty, value); }
    }

    public static PropertyInfo<string> CustomerNameProperty = RegisterProperty<string>(p => p.CustomerName);
    [Required]
    [Display(Name = "Customer name")]
    public string CustomerName
    {
      get { return GetProperty(CustomerNameProperty); }
      set { SetProperty(CustomerNameProperty, value); }
    }

    protected override void AddBusinessRules()
    {
      base.AddBusinessRules();
      //BusinessRules.AddRule(new MyExpensiveRule { PrimaryProperty = CustomerNameProperty });
    }

    public class MyExpensiveRule : Csla.Rules.BusinessRule
    {
      public MyExpensiveRule()
      {
        if (ApplicationContext.ExecutionLocation != ApplicationContext.ExecutionLocations.Server)
          IsAsync = true;
      }

      protected override void Execute(Csla.Rules.RuleContext context)
      {
        MyExpensiveCommand result = null;
        if (IsAsync)
        {
          MyExpensiveCommand.BeginCommand((r) =>
            {
              result = r;
              HandleResult(context, result);
            });
        }
        else
        {
          result = MyExpensiveCommand.DoCommand();
          HandleResult(context, result);
        }
      }

      private static void HandleResult(Csla.Rules.RuleContext context, MyExpensiveCommand result)
      {
        if (result == null)
          context.AddErrorResult("Command failed to run");
        else if (result.Result)
          context.AddInformationResult(result.ResultText);
        else
          context.AddErrorResult(result.ResultText);
        context.Complete();
      }
    }

    public static PropertyInfo<LineItems> LineItemsProperty = RegisterProperty<LineItems>(p => p.LineItems, RelationshipTypes.LazyLoad);
    public LineItems LineItems
    {
      get
      {
        if (!FieldManager.FieldExists(LineItemsProperty))
        {
          LoadProperty(LineItemsProperty, DataPortal.CreateChild<LineItems>());
          OnPropertyChanged(LineItemsProperty);
        }
        return GetProperty(LineItemsProperty);
      }
    }

    public static void NewOrder(EventHandler<DataPortalResult<Order>> callback)
    {
      DataPortal.BeginCreate<Order>(callback);
    }

    public static void GetOrder(int id, EventHandler<DataPortalResult<Order>> callback)
    {
      DataPortal.BeginFetch<Order>(id, callback);
    }

#if !SILVERLIGHT
    public static Order NewOrder()
    {
      return DataPortal.Create<Order>();
    }

    public static Order GetOrder(int id)
    {
      return DataPortal.Fetch<Order>(id);
    }

    public static void DeleteOrder(int id)
    {
      DataPortal.Delete<Order>(id);
    }
#endif
  }

  [Serializable]
  public class MyExpensiveCommand : CommandBase<MyExpensiveCommand>
  {
    public static PropertyInfo<bool> ResultProperty = RegisterProperty<bool>(c => c.Result);
    public bool Result
    {
      get { return ReadProperty(ResultProperty); }
      private set { LoadProperty(ResultProperty, value); }
    }

    public static PropertyInfo<string> ResultTextProperty = RegisterProperty<string>(c => c.ResultText);
    public string ResultText
    {
      get { return ReadProperty(ResultTextProperty); }
      private set { LoadProperty(ResultTextProperty, value); }
    }

    public static MyExpensiveCommand DoCommand()
    {
#if SILVERLIGHT
      throw new NotSupportedException("Method not valid in Silverlight");
#else
      var cmd = new MyExpensiveCommand();
      cmd = DataPortal.Execute<MyExpensiveCommand>(cmd);
      return cmd;
#endif
    }

    public static void BeginCommand(Action<MyExpensiveCommand> callback)
    {
      var cmd = new MyExpensiveCommand();
      DataPortal.BeginExecute<MyExpensiveCommand>(cmd, (o, e) =>
        {
          if (e.Error != null)
          {
            cmd.ResultText = e.Error.Message;
            cmd.Result = false;
          }
          else
          {
            cmd = e.Object;
          }
          callback(cmd);
        });
    }

#if !SILVERLIGHT
    protected override void DataPortal_Execute()
    {
      System.Threading.Thread.Sleep(5000);
      ResultText = "We're all good";
      Result = true;
    }
#endif
  }
}