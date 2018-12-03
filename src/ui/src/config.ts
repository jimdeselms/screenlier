export default class Config {
    public static getApiBaseUri(): string {
        return `${this.getBaseUri()}/api/v1`;
    }

    public static getImageBaseUri(): string {
        return `${this.getBaseUri()}/images`;
    }

    private static getBaseUri(): string {
        return process.env.REACT_APP_BASE_URL as string;
    }
} 