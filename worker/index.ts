import { Container, getContainer } from "@cloudflare/containers";

const portReadyTimeoutMs = 60_000;

export class WebContainer extends Container<Env> {
  defaultPort = 8080;
  sleepAfter = "10m";
  enableInternet = false;

  override async fetch(request: Request): Promise<Response> {
    const state = await this.getState();
    if (state.status !== "healthy") {
      await this.startAndWaitForPorts(this.defaultPort, {
        abort: request.signal,
        instanceGetTimeoutMS: 20_000,
        portReadyTimeoutMS: portReadyTimeoutMs,
      });
    }

    return this.containerFetch(request);
  }
}

export default {
  fetch(request: Request, env: Env): Promise<Response> {
    return getContainer(env.WEB).fetch(request);
  },
} satisfies ExportedHandler<Env>;
