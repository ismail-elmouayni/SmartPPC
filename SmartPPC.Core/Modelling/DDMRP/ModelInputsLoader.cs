using FluentResults;
using Newtonsoft.Json;
using DDMRP_AI.Core.Modelling.DDMRP;

namespace SmartPPC.Core.Modelling.DDMRP
{
    public class ModelInputsLoader
    {
        /// <summary>
        /// Import model input, such as station declarations, planning horizon etc. from config file
        /// </summary>
        /// <param name="configFilePath"> JSON Configuration file.</param>
        /// <returns> Configured <see cref="PpcModel"/></returns>
        public static Result<PpcModel> ImportModelInputs(string configFilePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(configFilePath);
                var configOptions = JsonConvert.DeserializeObject<ModelInputs>(jsonContent);

                if (configOptions == null)
                {
                    return Result.Fail<PpcModel>("Failed to read model inputs from json file.");
                }

                if (configOptions.StationDeclarations == null || configOptions.StationDeclarations.Count == 0)
                {
                    return Result.Fail<PpcModel>("No station declarations found in the model inputs file.");
                }

                var model = new PpcModel
                {
                    Stations = ImportStationsAndDemandInfo(configOptions.StationDeclarations, configOptions.PlanningHorizon).ToList(),
                    PeakHorizon = configOptions.PeakHorizon,
                    StationPrecedences = SetStationsPrecedences(configOptions.StationDeclarations),
                    StationInputPrecedences = SetStationsInputPrecedences(configOptions.StationDeclarations),
                    StationInitialBuffer = SetStationsInitialBuffer(configOptions.StationDeclarations)
                };


                return Result.Ok(model);
            }
            catch (Exception ex)
            {
                return Result.Fail<PpcModel>($"An error occurred while reading model inputs from file : {ex.Message}");
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
                    stationsPrecedences[precedence.StationIndex!.Value] = isStationNext;
                    continue;
                }

                foreach (var input in precedence.NextStationsInput)
                {
                    isStationNext[input.NextStationIndex] = 1;
                }

                stationsPrecedences[precedence.StationIndex!.Value] = isStationNext;
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
                    stationsInputPrecedences[precedence.StationIndex!.Value] = nextStationInput;
                    continue;
                }

                foreach (var input in precedence.NextStationsInput)
                {
                    nextStationInput[input.NextStationIndex] = input.InputAmount;
                }

                stationsInputPrecedences[precedence.StationIndex!.Value] = nextStationInput;
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
                    stationsBuffersSizes[buffer.StationIndex!.Value] = buffer.InitialBuffer.Value;
                }
            }

            return stationsBuffersSizes;
        }

        private static IOrderedEnumerable<Station> ImportStationsAndDemandInfo(List<StationDeclaration> stationDeclarations,
            int planningHorizon)
        {
            var inputStationsIndex = stationDeclarations.Where(d => d.NextStationsInput != null)
                .SelectMany(d => d.NextStationsInput!.Select(ni => ni.NextStationIndex));
            
            var badDemandForecastDeclarations =
                stationDeclarations.Where(d => d.DemandForecast != null && d.DemandForecast.Count != planningHorizon)
                    .ToList();

            if (badDemandForecastDeclarations.Any())
            {
                throw new InvalidDataException($"Demand forecast size different from planning horizon for output stations " +
                                               $"{string.Join(",", badDemandForecastDeclarations.Select(d => "(station : "+ d.StationIndex + ", PH :"+ d.DemandForecast!.Count +")" ))}");
            }

            var outputStationWithoutDemandVariability =
                stationDeclarations.Where(d => d.NextStationsInput is null && d.DemandVariability is null)
                    .ToList();

            if (outputStationWithoutDemandVariability.Any())
            {
                throw new InvalidDataException("Demand variability for output station not set properly for stations : " +
                                               $"{string.Join(",", outputStationWithoutDemandVariability.Select(d => d.StationIndex))}");
            }

            var stations = stationDeclarations
                .Select(s => new Station
                {
                    Index = s.StationIndex ?? throw new InvalidDataException("Station index of one of the station declaration not declared"),
                    IsOutputStation = s.NextStationsInput is null,
                    IsInputStation = !inputStationsIndex.Contains(s.StationIndex.Value),
                    DemandVariability = s.DemandVariability,
                    ProcessingTime = s.ProcessingTime ?? throw new InvalidDataException($"Processing time for station {s.StationIndex} not declared"),

                    StateTimeLine = Enumerable.Range(0, planningHorizon)
                        .Select(t => new TimeIndexedStationState
                        {
                            Instant = t,
                            Demand =  s.DemandForecast?.ElementAt(t)
                        })
                        .OrderBy(s => s.Instant)
                        .ToList()
                });

            return stations.OrderBy(s => s.Index);
        }
    }
}
