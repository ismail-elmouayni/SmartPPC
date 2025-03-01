using FluentResults;
using Newtonsoft.Json;
using DDMRP_AI.Core.Modelling.DDMRP;

namespace SmartPPC.Core.Modelling.DDMRP
{
    public class DDMRP_ModelConfigurator
    {
        /// <summary>
        /// Import model input, such as station declarations, planning horizon etc. from config file
        /// </summary>
        /// <param name="configFilePath"> JSON Configuration file.</param>
        /// <returns> Configured <see cref="DDMRP_Model"/></returns>
        public static Result<DDMRP_Model> ImportModelInputs(string configFilePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                var configOptions = JsonConvert.DeserializeObject<DDMRP_ConfigOptions>(jsonContent);

                if (configOptions == null)
                {
                    return Result.Fail<DDMRP_Model>("Failed to deserialize Model config options.");
                }

                if (configOptions.StationDeclarations == null || configOptions.StationDeclarations.Count == 0)
                {
                    return Result.Fail<DDMRP_Model>("No station declarations found in the config file.");
                }

                var model = new DDMRP_Model
                {
                    PeakHorizon = configOptions.PeakHorizon,
                    OutputStationDemandVariability = configOptions.OutputStationDemandVariability,

                    StationPrecedences = SetStationsPrecedences(configOptions.StationDeclarations),
                    StationInputPrecedences = SetStationsInputPrecedences(configOptions.StationDeclarations),
                    StationInitialBuffer = SetStationsInitialBuffer(configOptions.StationDeclarations),

                    Stations = ImportStations(configOptions.StationDeclarations, configOptions.PlanningHorizon)
                };


                return Result.Ok(model);
            }
            catch (Exception ex)
            {
                return Result.Fail<DDMRP_Model>($"An error occurred while configuring the model: {ex.Message}");
            }
        }

        private static int[][] SetStationsPrecedences(List<StationDeclaration> stationDeclarations)
        {
            // Indicates if a station i and j are connected i.e. stationsPrecedences[i][j] = 1
            var stationsPrecedences = new int[stationDeclarations.Count][];

            foreach (var precedence in stationDeclarations)
            {
                var isStationNext = new int[stationDeclarations.Count];

                if (precedence.NextStationsInput == null || precedence.NextStationsInput.Count == 0)
                {
                    stationsPrecedences[precedence.StationIndex] = isStationNext;
                    continue;
                }

                foreach (var input in precedence.NextStationsInput)
                {
                    isStationNext[input.NextStationIndex] = 1;
                }

                stationsPrecedences[precedence.StationIndex] = isStationNext;
            }

            return stationsPrecedences;
        }

        private static int[][] SetStationsInputPrecedences(List<StationDeclaration> stationDeclarations)
        {
            // Indicates the input amount from station i to j i.e. stationsInputPrecedences[i][j] = inputAmount
            var stationsInputPrecedences = new int[stationDeclarations.Count][];

            foreach (var precedence in stationDeclarations)
            {
                var nextStationInput = new int[stationDeclarations.Count];

                if (precedence.NextStationsInput == null || precedence.NextStationsInput.Count == 0)
                {
                    stationsInputPrecedences[precedence.StationIndex] = nextStationInput;
                    continue;
                }

                foreach (var input in precedence.NextStationsInput)
                {
                    nextStationInput[input.NextStationIndex] = input.InputAmount;
                }

                stationsInputPrecedences[precedence.StationIndex] = nextStationInput;
            }

            return stationsInputPrecedences;
        }


        private static int[] SetStationsInitialBuffer(List<StationDeclaration> stationDeclarations)
        {
            var stationsBuffersSizes = new int[stationDeclarations.Count];

            foreach (var buffer in stationDeclarations)
            {
                if (buffer.InitialBuffer.HasValue)
                {
                    stationsBuffersSizes[buffer.StationIndex] = buffer.InitialBuffer.Value;
                }
            }

            return stationsBuffersSizes;
        }

        private static IOrderedEnumerable<Station> ImportStations(List<StationDeclaration> stationDeclarations,
            int planningHorizon)
        {
            var stations = Enumerable.Range(0, stationDeclarations.Count)
                .Select(s => new Station
                {
                    Index = s,
                    StateTimeLine = Enumerable.Range(0, planningHorizon)
                        .Select(t => new TimeIndexedStationState
                        {
                            Instant = t
                        })
                        .OrderBy(s => s.Instant)
                });

            return stations.OrderBy(s => s.Index);
        }
    }
}
