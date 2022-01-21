#!/usr/bin/python3
# add main importer loop in pyhton (also able to run distributed)
#   run import loop every x seconds (adjust sleep time per management depending on the change frequency )
#      import_mgm.py

import signal
import argparse
import sys
import time
import json
import logging
import requests
import common, fwo_api

# https://stackoverflow.com/questions/18499497/how-to-process-sigterm-signal-gracefully
class GracefulKiller:
    kill_now = False

    def __init__(self):
        signal.signal(signal.SIGINT, self.exit_gracefully)
        signal.signal(signal.SIGTERM, self.exit_gracefully)

    def exit_gracefully(self, *args):
        self.kill_now = True


if __name__ == '__main__':
    base_dir = "/usr/local/fworch"
    importer_base_dir = base_dir + '/importer'
    sys.path.append(importer_base_dir)
    parser = argparse.ArgumentParser(
        description='Run import loop across all managements to read configuration from FW managements via API calls')
    parser.add_argument('-i', '--interval', metavar='interval_time',
                        default=90, help='time in seconds to sleep between import loops')
    parser.add_argument('-d', '--debug', metavar='debug_level', default='0',
                        help='Debug Level: 0=off, 1=send debug to console, 2=send debug to file, 3=keep temporary config files; default=0')
    parser.add_argument('-x', '--proxy', metavar='proxy_string',
                        help='proxy server string to use, e.g. http://1.2.3.4:8080')
    parser.add_argument('-s', '--ssl', metavar='ssl_verification_mode', default='',
                        help='[ca]certfile, if value not set, ssl check is off"; default=empty/off')
    parser.add_argument('-l', '--limit', metavar='api_limit', default='150',
                        help='The maximal number of returned results per HTTPS Connection; default=150')

    args = parser.parse_args()

    requests.packages.urllib3.disable_warnings()  # suppress ssl warnings only

    importer_user_name = 'importer'  # todo: move to config file?
    fwo_config_filename = base_dir + '/etc/fworch.json'
    importer_pwd_file = base_dir + '/etc/secrets/importer_pwd'
    debug_level = int(args.debug)
    common.set_log_level(log_level=debug_level, debug_level=debug_level)

    # read fwo config (API URLs)
    with open(fwo_config_filename, "r") as fwo_config:
        fwo_config = json.loads(fwo_config.read())
    user_management_api_base_url = fwo_config['middleware_uri']
    fwo_api_base_url = fwo_config['api_uri']
    killer = GracefulKiller()
    while not killer.kill_now:
        # authenticate to get JWT
        with open(importer_pwd_file, 'r') as file:
            importer_pwd = file.read().replace('\n', '')
        jwt = fwo_api.login(importer_user_name, importer_pwd,
                            user_management_api_base_url, ssl_verification=args.ssl, proxy=args.proxy)

        mgm_ids = fwo_api.get_mgm_ids(fwo_api_base_url, jwt, {})
        api_fetch_limit = fwo_api.get_config_value(
            fwo_api_base_url, jwt, key='fwApiElementsPerFetch')
        if api_fetch_limit == None:
            api_fetch_limit = '150'

        for mgm_id in mgm_ids:
            if killer.kill_now:
                break
            id = str(mgm_id['id'])
            # getting a new JWT in case the old one is not valid anymore after a long previous import
            jwt = fwo_api.login(importer_user_name, importer_pwd,
                                user_management_api_base_url, ssl_verification=args.ssl, proxy=args.proxy)
            mgm_details = fwo_api.get_mgm_details(
                fwo_api_base_url, jwt, {"mgmId": id})
            if mgm_details["deviceType"]["id"] in (9, 11):  # only handle CPR8x and fortiManager
                #logging.info("import-main-loop: starting import of mgm_id=" + id)
                try:
                    import_result = common.import_management(mgm_id=id, ssl=args.ssl, debug_level=debug_level, limit=api_fetch_limit)
                except BaseException:
                    tb = sys.exc_info()[2]
                    #logging.error("vars: " + str(tb.tb_frame.f_locals))
        logging.info("import-main-loop.py: sleeping between loops for " +
                     str(args.interval) + " seconds")
        counter=0
        while counter < int(args.interval) and not killer.kill_now:
            time.sleep(1)
            counter += 1

    print("importer-api was killed gracefully.")
