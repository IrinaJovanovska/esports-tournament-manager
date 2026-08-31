using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace MatchResultUpdatePlugin.Plugins
{
    public class MatchResultUpdatePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            // =====================================================
            // TRACING SERVICE
            // =====================================================

            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(
                    typeof(ITracingService));

            tracingService.Trace(
                "=====================================================");

            tracingService.Trace(
                "MatchResultUpdatePlugin STARTED.");

            // =====================================================
            // PLUGIN CONTEXT
            // =====================================================

            IPluginExecutionContext context =
                (IPluginExecutionContext)serviceProvider.GetService(
                    typeof(IPluginExecutionContext));

            tracingService.Trace(
                "MessageName: {0}",
                context.MessageName);

            tracingService.Trace(
                "PrimaryEntityName: {0}",
                context.PrimaryEntityName);

            tracingService.Trace(
                "Stage: {0}",
                context.Stage);

            tracingService.Trace(
                "Mode: {0}",
                context.Mode);

            // =====================================================
            // ORGANIZATION SERVICE
            // =====================================================

            IOrganizationServiceFactory serviceFactory =
                (IOrganizationServiceFactory)serviceProvider.GetService(
                    typeof(IOrganizationServiceFactory));

            IOrganizationService service =
                serviceFactory.CreateOrganizationService(context.UserId);

            try
            {
                tracingService.Trace(
                    "Entered TRY block.");

                // =====================================================
                // ONLY UPDATE
                // =====================================================

                if (!context.MessageName.Equals(
                    "Update",
                    StringComparison.OrdinalIgnoreCase))
                {
                    tracingService.Trace(
                        "RETURN: Message is not Update.");

                    return;
                }

                tracingService.Trace(
                    "Message is Update.");

                // =====================================================
                // CHECK TARGET
                // =====================================================

                if (!context.InputParameters.Contains("Target") ||
                    !(context.InputParameters["Target"] is Entity))
                {
                    tracingService.Trace(
                        "RETURN: Target is missing or is not Entity.");

                    return;
                }

                Entity targetEntity =
                    (Entity)context.InputParameters["Target"];

                tracingService.Trace(
                    "Target LogicalName: {0}",
                    targetEntity.LogicalName);

                // =====================================================
                // MAKE SURE THIS IS sol_match
                // =====================================================

                if (targetEntity.LogicalName != "sol_match")
                {
                    tracingService.Trace(
                        "RETURN: Target is not sol_match.");

                    return;
                }

                tracingService.Trace(
                    "Target is sol_match.");

                // =====================================================
                // WE ONLY CARE WHEN sol_winner IS UPDATED
                // =====================================================

                if (!targetEntity.Contains("sol_winner"))
                {
                    tracingService.Trace(
                        "RETURN: sol_winner is not present in Target.");

                    return;
                }

                tracingService.Trace(
                    "sol_winner IS present in Target.");

                // =====================================================
                // GET NEW WINNER
                // =====================================================

                EntityReference newWinner =
                    targetEntity.GetAttributeValue<EntityReference>(
                        "sol_winner");

                if (newWinner == null)
                {
                    tracingService.Trace(
                        "RETURN: sol_winner is NULL.");

                    return;
                }

                tracingService.Trace(
                    "New Winner ID: {0}",
                    newWinner.Id);

                // =====================================================
                // GET PRE IMAGE
                // =====================================================

                if (!context.PreEntityImages.Contains("PreImage"))
                {
                    tracingService.Trace(
                        "ERROR: PreImage is missing.");

                    throw new InvalidPluginExecutionException(
                        "PreImage is required for MatchResultUpdatePlugin.");
                }

                tracingService.Trace(
                    "PreImage found.");

                Entity preImage =
                    context.PreEntityImages["PreImage"];

               

             

                // =====================================================
                // GET OLD WINNER
                // =====================================================

                EntityReference oldWinner =
                    preImage.GetAttributeValue<EntityReference>(
                        "sol_winner");

                tracingService.Trace(
                    "Old Winner: {0}",
                    oldWinner == null
                        ? "NULL"
                        : oldWinner.Id.ToString());

                // =====================================================
                // MAKE SURE MATCH DID NOT ALREADY HAVE A WINNER
                // =====================================================

                if (oldWinner != null)
                {
                    tracingService.Trace(
                        "RETURN: Match already had a winner.");

                    return;
                }

                tracingService.Trace(
                    "Match did not have a previous winner.");

                // =====================================================
                // GET TOURNAMENT
                // =====================================================

                EntityReference tournamentRef =
                    preImage.GetAttributeValue<EntityReference>(
                        "sol_tournament");

                tracingService.Trace(
                    "Tournament ID: {0}",
                    tournamentRef == null
                        ? "NULL"
                        : tournamentRef.Id.ToString());

                if (tournamentRef == null)
                {
                    tracingService.Trace(
                        "RETURN: Tournament is missing.");

                    return;
                }

                // =====================================================
                // GET TEAM A
                // =====================================================

                EntityReference teamA =
                    preImage.GetAttributeValue<EntityReference>(
                        "sol_teama");

                tracingService.Trace(
                    "Team A ID: {0}",
                    teamA == null
                        ? "NULL"
                        : teamA.Id.ToString());

                // =====================================================
                // GET TEAM B
                // =====================================================

                EntityReference teamB =
                    preImage.GetAttributeValue<EntityReference>(
                        "sol_teamb");

                tracingService.Trace(
                    "Team B ID: {0}",
                    teamB == null
                        ? "NULL"
                        : teamB.Id.ToString());

                if (teamA == null || teamB == null)
                {
                    tracingService.Trace(
                        "RETURN: Team A or Team B is missing.");

                    return;
                }

                // =====================================================
                // DETERMINE LOSER
                // =====================================================

                EntityReference loser;

                if (newWinner.Id == teamA.Id)
                {
                    loser = teamB;

                    tracingService.Trace(
                        "Winner = Team A.");

                    tracingService.Trace(
                        "Loser = Team B.");
                }
                else if (newWinner.Id == teamB.Id)
                {
                    loser = teamA;

                    tracingService.Trace(
                        "Winner = Team B.");

                    tracingService.Trace(
                        "Loser = Team A.");
                }
                else
                {
                    tracingService.Trace(
                        "ERROR: Winner is neither Team A nor Team B.");

                    throw new InvalidPluginExecutionException(
                        "Winner must be either Team A or Team B.");
                }

                // =====================================================
                // UPDATE WINNER STANDING
                // =====================================================

                tracingService.Trace(
                    "Calling UpdateTournamentStanding for WINNER.");

                UpdateTournamentStanding(
                    service,
                    tournamentRef.Id,
                    newWinner.Id,
                    true);

                tracingService.Trace(
                    "Winner standing updated successfully.");

                // =====================================================
                // UPDATE LOSER STANDING
                // =====================================================

                tracingService.Trace(
                    "Calling UpdateTournamentStanding for LOSER.");

                UpdateTournamentStanding(
                    service,
                    tournamentRef.Id,
                    loser.Id,
                    false);

                tracingService.Trace(
                    "Loser standing updated successfully.");

                // =====================================================
                // UPDATE COMPLETED MATCHES
                // =====================================================

                tracingService.Trace(
                    "Calling UpdateTournamentCompletedMatches.");

                UpdateTournamentCompletedMatches(
                    service,
                    tournamentRef.Id);

                tracingService.Trace(
                    "Tournament completed matches updated successfully.");

                // =====================================================
                // FINISHED
                // =====================================================

                tracingService.Trace(
                    "MatchResultUpdatePlugin FINISHED SUCCESSFULLY.");

                tracingService.Trace(
                    "=====================================================");
            }
            catch (Exception ex)
            {
                tracingService.Trace(
                    "=====================================================");

                tracingService.Trace(
                    "MatchResultUpdatePlugin EXCEPTION.");

                tracingService.Trace(
                    "Exception Message: {0}",
                    ex.Message);

                tracingService.Trace(
                    "Exception Details: {0}",
                    ex.ToString());

                tracingService.Trace(
                    "=====================================================");

                throw new InvalidPluginExecutionException(
                    "Error in MatchResultUpdatePlugin: " +
                    ex.Message,
                    ex);
            }
        }


        // =============================================================
        // UPDATE TOURNAMENT STANDING
        // =============================================================

        private void UpdateTournamentStanding(
            IOrganizationService service,
            Guid tournamentId,
            Guid teamId,
            bool isWinner)
        {
            QueryExpression query =
                new QueryExpression(
                    "sol_tournamentstanding");

            query.ColumnSet =
                new ColumnSet(
                    "sol_tournament",
                    "sol_team",
                    "sol_wins",
                    "sol_losses",
                    "sol_points");

            query.Criteria.AddCondition(
                "sol_tournament",
                ConditionOperator.Equal,
                tournamentId);

            query.Criteria.AddCondition(
                "sol_team",
                ConditionOperator.Equal,
                teamId);

            EntityCollection standings =
                service.RetrieveMultiple(query);

            // =========================================================
            // CREATE STANDING IF IT DOES NOT EXIST
            // =========================================================

            if (standings.Entities.Count == 0)
            {
                Entity newStanding =
                    new Entity(
                        "sol_tournamentstanding");

                newStanding["sol_tournament"] =
                    new EntityReference(
                        "sol_tournament",
                        tournamentId);

                newStanding["sol_team"] =
                    new EntityReference(
                        "sol_team",
                        teamId);

                newStanding["sol_wins"] =
                    isWinner ? 1 : 0;

                newStanding["sol_losses"] =
                    isWinner ? 0 : 1;

                newStanding["sol_points"] =
                    isWinner ? 3 : 0;

                service.Create(newStanding);

                return;
            }

            // =========================================================
            // UPDATE EXISTING STANDING
            // =========================================================

            Entity standing =
                standings.Entities[0];

            int wins =
                standing.GetAttributeValue<int?>(
                    "sol_wins") ?? 0;

            int losses =
                standing.GetAttributeValue<int?>(
                    "sol_losses") ?? 0;

            int points =
                standing.GetAttributeValue<int?>(
                    "sol_points") ?? 0;

            // =========================================================
            // INCREASE WINS OR LOSSES
            // =========================================================

            if (isWinner)
            {
                wins++;
                points += 3;
            }
            else
            {
                losses++;
            }

            Entity updateStanding = new Entity("sol_tournamentstanding")
            {
                Id = standing.Id
            };

            updateStanding["sol_wins"] =
                wins;

            updateStanding["sol_losses"] =
                losses;

            updateStanding["sol_points"] =
                points;

            service.Update(updateStanding);
        }


        // =============================================================
        // UPDATE COMPLETED MATCHES AND TOURNAMENT STATUS
        // =============================================================

        private void UpdateTournamentCompletedMatches(
            IOrganizationService service,
            Guid tournamentId)
        {
            // =========================================================
            // GET TOURNAMENT DATA
            // =========================================================

            Entity tournament =
                service.Retrieve(
                    "sol_tournament",
                    tournamentId,
                    new ColumnSet(
                        "sol_totalmatches",
                        "sol_completedmatches",
                        "sol_status"));

            int totalMatches =
                tournament.GetAttributeValue<int?>(
                    "sol_totalmatches") ?? 0;

            int completedMatches =
                tournament.GetAttributeValue<int?>(
                    "sol_completedmatches") ?? 0;

            // =========================================================
            // INCREASE COMPLETED MATCHES
            // =========================================================

            int newCompletedMatches =
                completedMatches + 1;

            Entity updateTournament = new Entity("sol_tournament")
            {
                Id = tournamentId
            };

            updateTournament["sol_completedmatches"] =
                newCompletedMatches;

            service.Update(updateTournament);

            // =========================================================
            // FINAL MATCH
            // =========================================================

            if (totalMatches > 0 &&
                newCompletedMatches == totalMatches)
            {
                Entity completeTournament = new Entity("sol_tournament")
                {
                    Id = tournamentId
                };

                // Completed = 3
                completeTournament["sol_status"] =
                    new OptionSetValue(3);

                service.Update(completeTournament);
            }
        }
    }
}