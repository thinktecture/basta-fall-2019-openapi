/**
 * OpenAPI Sample API v2
 * Sample API for an OpenAPI conference talk.</br>This document was automatically generated as of 25.09.2019 06:32:09.
 *
 * OpenAPI spec version: v2
 * Contact: sebastian.gingter@thinktecture.com
 *
 * NOTE: This class is auto generated by the swagger code generator program.
 * https://github.com/swagger-api/swagger-codegen.git
 * Do not edit the class manually.
 */import { WeatherForecast } from './weatherForecast';


/**
 * Contains a paged result of different data entries.
 */
export interface WeatherForecastPagedResult { 
    /**
     * The forecasts.
     */
    entries?: Array<WeatherForecast>;
    /**
     * The starting index of entries returned.
     */
    startIndex?: number;
    /**
     * The total amount of available entries.
     */
    totalAmount?: number;
}