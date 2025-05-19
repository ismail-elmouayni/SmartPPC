using FluentResults;
using Newtonsoft.Json;

namespace SmartPPC.Core.Model.DDMRP
{
    public class ModelBuilder
    {
        public static Result<ModelInputs> GetInputsFromFile(string modelInputsFilePath)
        {
            try
            {
                string jsonContent = File.ReadAllText(modelInputsFilePath);
                var inputs = JsonConvert.DeserializeObject<ModelInputs>(jsonContent);

                if (inputs == null)
                {
                    return Result.Fail<ModelInputs>("Failed to read model inputs from json file.");
                }

                if (inputs.StationDeclarations == null || inputs.StationDeclarations.Count == 0)
                {
                    return Result.Fail<ModelInputs>("No station declarations found in the model inputs file.");
                }

                return Result.Ok(inputs);
            }
            catch (Exception ex)
            {
                return Result.Fail<ModelInputs>($"An error occurred while reading model inputs from file : {ex.Message}");
            }
        }

        /// <summary>
        /// Import model input, such as station declarations, planning horizon etc. from config file
        /// </summary>
        /// <param name="inputsFilePath"> JSON Configuration file.</param>
        /// <returns> Configured <see cref="ProductionControlModel"/></returns>
        public static Result<ProductionControlModel> CreateFromFile(string inputsFilePath)
        {
            try
            {
                var getInputsResult = GetInputsFromFile(inputsFilePath);
                
                if(getInputsResult.IsFailed)
                {
                    return Result.Fail<ProductionControlModel>(getInputsResult.Errors);
                }

                return CreateFromInputs(getInputsResult.Value);
            }
            catch (Exception ex)
            {
                return Result.Fail<ProductionControlModel>($"An error occurred while reading model inputs from file : {ex.Message}");
            }
        }

        public static Result<ProductionControlModel> CreateFromInputs(ModelInputs inputs)
        {
            var stations = ImportStationsAndDemandInfo(
                inputs.StationDeclarations,
                inputs.PlanningHorizon,
                inputs.PastHorizon).ToList();

            var stationPrecedences = SetStationsPrecedences(inputs.StationDeclarations);
            var stationInputPrecedences = SetStationsInputPrecedences(inputs.StationDeclarations);
            var stationInitialBuffer = SetStationsInitialBuffer(inputs.StationDeclarations);

            var model = new ProductionControlModel(
                stations: stations,
                stationPrecedences: stationPrecedences,
                stationInputPrecedences: stationInputPrecedences,
                stationInitialBuffer: stationInitialBuffer,
                peakHorizon: inputs.PeakHorizon,
                planningHorizon: inputs.PlanningHorizon,
                pastHorizon: inputs.PastHorizon,
                peakThreshold: inputs.PeakThreshold
            );

            return Result.Ok(model);
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

        private static IOrderedEnumerable<StationModel> ImportStationsAndDemandInfo(
            List<StationDeclaration> stationDeclarations,
            int planningHorizon,
            int pastHorizon)
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
                .Select(dec => new StationModel
                {
                    Index = dec.StationIndex ?? throw new InvalidDataException("Station index of one of the station declaration not declared"),
                    IsOutputStation = dec.NextStationsInput is null,
                    IsInputStation = !inputStationsIndex.Contains(dec.StationIndex.Value),
                    LeadTime = !inputStationsIndex.Contains(dec.StationIndex.Value) ? dec.LeadTime : null,
                    DemandVariability = dec.DemandVariability,
                    ProcessingTime = dec.ProcessingTime ?? throw new InvalidDataException($"Processing time for station {dec.StationIndex} not declared"),
                    DemandForecast = dec.DemandForecast?.ToArray(),

                    PastStates = Enumerable.Range(0, pastHorizon)
                        .Select(t => new TimeIndexedPastState
                        {
                            Instant = -pastHorizon+1+t,
                            Buffer = dec.PastBuffer[t],
                            OrderAmount = dec.PastOrderAmount[t]
                        })
                        .OrderBy(s => s.Instant)
                        .ToList()
                });

            return stations.OrderBy(s => s.Index);
        }
    }
}
