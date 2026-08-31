using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace LowRatingFeedback.Plugins
{
    public class LowRatingFeedback : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext));

            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(
                    typeof(ITracingService));

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(
                    typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            tracingService.Trace(
                "========== LowRatingFeedbackNotificationPlugin STARTED ==========");

            tracingService.Trace(
                $"MessageName: {context.MessageName}");

            tracingService.Trace(
                $"PrimaryEntityName: {context.PrimaryEntityName}");

            tracingService.Trace(
                $"Stage: {context.Stage}");

            // =====================================================
            // CHECK MESSAGE
            // =====================================================

            if (context.MessageName != "Create" &&
                context.MessageName != "Update")
            {
                tracingService.Trace(
                    "Message is not Create or Update. Exiting.");

                return;
            }

            // =====================================================
            // CHECK ENTITY
            // =====================================================

            if (context.PrimaryEntityName != "sol_feedback")
            {
                tracingService.Trace(
                    "Entity is not sol_feedback. Exiting.");

                return;
            }

            // =====================================================
            // GET TARGET
            // =====================================================

            if (!context.InputParameters.Contains("Target") ||
                !(context.InputParameters["Target"] is Entity))
            {
                tracingService.Trace(
                    "Target not found. Exiting.");

                return;
            }

            Entity feedback =
                (Entity)context.InputParameters["Target"];

            tracingService.Trace(
                $"Target LogicalName: {feedback.LogicalName}");

            tracingService.Trace(
                $"Target ID: {feedback.Id}");

            // =====================================================
            // GET RATING
            // =====================================================

            int rating;

            if (context.MessageName == "Create")
            {
                tracingService.Trace(
                    "Processing CREATE.");

                if (!feedback.Contains("sol_rating"))
                {
                    tracingService.Trace(
                        "sol_rating is not present in Target. Exiting.");

                    return;
                }

                rating =
                    feedback.GetAttributeValue<int>("sol_rating");

                tracingService.Trace(
                    $"Create Rating: {rating}");
            }
            else
            {
                tracingService.Trace(
                    "Processing UPDATE.");

                if (!feedback.Contains("sol_rating"))
                {
                    tracingService.Trace(
                        "sol_rating is not present in Update Target. Exiting.");

                    return;
                }

                rating =
                    feedback.GetAttributeValue<int>("sol_rating");

                tracingService.Trace(
                    $"New Rating: {rating}");

                // =================================================
                // CHECK PREIMAGE
                // =================================================

                if (context.PreEntityImages.Contains("PreImage"))
                {
                    Entity preImage =
                        context.PreEntityImages["PreImage"];

                    if (preImage.Contains("sol_rating"))
                    {
                        int oldRating =
                            preImage.GetAttributeValue<int>("sol_rating");

                        tracingService.Trace(
                            $"Old Rating: {oldRating}");

                        if (oldRating == rating)
                        {
                            tracingService.Trace(
                                "Rating was not changed. Exiting.");

                            return;
                        }
                    }
                }
                else
                {
                    tracingService.Trace(
                        "PreImage not found.");
                }
            }

            // =====================================================
            // LOW RATING CHECK
            // =====================================================

            tracingService.Trace(
                $"Final Rating: {rating}");

            if (rating > 2)
            {
                tracingService.Trace(
                    "Rating is not low. No notification needed.");

                return;
            }

            tracingService.Trace(
                "LOW RATING DETECTED!");

            // =====================================================
            // FIND TOURNAMENT MANAGER ROLE
            // =====================================================

            tracingService.Trace(
                "Searching for Tournament Manager security role...");

            QueryExpression roleQuery =
                new QueryExpression("role");

            roleQuery.ColumnSet =
                new ColumnSet("name");

            roleQuery.Criteria.AddCondition(
                "name",
                ConditionOperator.Equal,
                "Tournament Manager");

            EntityCollection roles =
                service.RetrieveMultiple(roleQuery);

            tracingService.Trace(
                $"Tournament Manager roles found: {roles.Entities.Count}");

            if (roles.Entities.Count == 0)
            {
                tracingService.Trace(
                    "Tournament Manager role was NOT found.");

                return;
            }

            // =====================================================
            // FIND USERS WITH TOURNAMENT MANAGER ROLE
            // =====================================================

            foreach (Entity role in roles.Entities)
            {
                tracingService.Trace(
                    $"Tournament Manager Role ID: {role.Id}");

                tracingService.Trace(
                    $"Tournament Manager Role Name: " +
                    $"{role.GetAttributeValue<string>("name")}");

                QueryExpression userRoleQuery =
                    new QueryExpression("systemuser");

                userRoleQuery.ColumnSet =
                    new ColumnSet(
                        "fullname",
                        "internalemailaddress");

                LinkEntity roleLink =
                    userRoleQuery.AddLink(
                        "systemuserroles",
                        "systemuserid",
                        "systemuserid");

                roleLink.LinkCriteria.AddCondition(
                    "roleid",
                    ConditionOperator.Equal,
                    role.Id);

                EntityCollection users =
                    service.RetrieveMultiple(userRoleQuery);

                tracingService.Trace(
                    $"Users with Tournament Manager role: " +
                    $"{users.Entities.Count}");

                // =================================================
                // SEND APP NOTIFICATION
                // =================================================

                foreach (Entity user in users.Entities)
                {
                    string fullName =
                        user.GetAttributeValue<string>("fullname");

                    string email =
                        user.GetAttributeValue<string>(
                            "internalemailaddress");

                    tracingService.Trace(
                        "----------------------------------------");

                    tracingService.Trace(
                        $"Tournament Manager User ID: {user.Id}");

                    tracingService.Trace(
                        $"Tournament Manager Name: {fullName}");

                    tracingService.Trace(
                        $"Tournament Manager Email: {email}");

                    // =================================================
                    // CREATE SEND APP NOTIFICATION REQUEST
                    // =================================================

                    tracingService.Trace(
                        "Creating SendAppNotification request...");

                    OrganizationRequest notificationRequest =
                        new OrganizationRequest();

                    notificationRequest.RequestName =
                        "SendAppNotification";

                    // =================================================
                    // TITLE
                    // =================================================

                    notificationRequest["Title"] =
                        "Low Rating Feedback";

                    // =================================================
                    // RECIPIENT
                    // =================================================

                    notificationRequest["Recipient"] =
                        new EntityReference(
                            "systemuser",
                            user.Id);

                    // =================================================
                    // BODY
                    // =================================================

                    notificationRequest["Body"] =
                        $"A feedback was submitted with a low rating ({rating}).";

                    // =================================================
                    // VIEW FEEDBACK ACTION
                    // =================================================

                    tracingService.Trace(
                        $"Creating View Feedback action for Feedback ID: " +
                        $"{feedback.Id}");

                    Entity actions =
                        new Entity();

                    EntityCollection actionCollection =
                        new EntityCollection();

                    Entity viewFeedbackAction =
                        new Entity();

                    viewFeedbackAction["title"] =
                        "View Feedback";

                    Entity actionData =
                        new Entity();

                    actionData["type"] =
                        "url";

                    // Open the specific sol_feedback record
                    actionData["url"] =
                        "?pagetype=entityrecord" +
                        "&etn=sol_feedback" +
                        "&id=" +
                        feedback.Id.ToString();

                    actionData["navigationTarget"] =
                        "newWindow";

                    viewFeedbackAction["data"] =
                        actionData;

                    actionCollection.Entities.Add(
                        viewFeedbackAction);

                    actions["actions"] =
                        actionCollection;

                    notificationRequest["Actions"] =
                        actions;

                    tracingService.Trace(
                        "View Feedback action created successfully.");

                    // =================================================
                    // ICON
                    // =================================================

                    // Warning icon
                    notificationRequest["IconType"] =
                        new OptionSetValue(100000003);

                    // =================================================
                    // TOAST TYPE
                    // =================================================

                    // Timed notification
                    notificationRequest["ToastType"] =
                        new OptionSetValue(200000000);

                    // =================================================
                    // EXECUTE NOTIFICATION
                    // =================================================

                    tracingService.Trace(
                        "Executing SendAppNotification...");

                    OrganizationResponse response =
                        service.Execute(notificationRequest);

                    tracingService.Trace(
                        "SendAppNotification executed successfully.");

                    // =================================================
                    // GET NOTIFICATION ID
                    // =================================================

                    if (response.Results.Contains("NotificationId"))
                    {
                        Guid notificationId =
                            (Guid)response.Results["NotificationId"];

                        tracingService.Trace(
                            $"Notification ID: {notificationId}");
                    }
                    else
                    {
                        tracingService.Trace(
                            "NotificationId was not returned.");
                    }

                    tracingService.Trace(
                        "----------------------------------------");
                }
            }

            // =====================================================
            // FINISHED
            // =====================================================

            tracingService.Trace(
                "Tournament Manager lookup and notification completed.");

            tracingService.Trace(
                "========== LowRatingFeedbackNotificationPlugin FINISHED ==========");
        }
    }
}