export * from './article.service';
import { ArticleService } from './article.service';
export * from './changed.service';
import { ChangedService } from './changed.service';
export * from './health.service';
import { HealthService } from './health.service';
export * from './weather.service';
import { WeatherService } from './weather.service';
export const APIS = [ArticleService, ChangedService, HealthService, WeatherService];
